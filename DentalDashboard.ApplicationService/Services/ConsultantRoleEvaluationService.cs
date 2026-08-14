using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.RolePolicies;
using DentalDashboard.Framwork.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DentalDashboard.ApplicationService.Services;

public sealed class ConsultantRoleEvaluationService : IConsultantRoleEvaluationService
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> ConsultantLocks = new();
    private readonly IConsultantProfileRepository profiles;
    private readonly IReservationRepository reservations;
    private readonly IBaseRepository<long, ConsultantRoleEvaluation> evaluations;
    private readonly IConsultantRolePolicyProvider policies;
    private readonly IUnitOfWork unitOfWork;

    public ConsultantRoleEvaluationService(
        IConsultantProfileRepository profiles,
        IReservationRepository reservations,
        IBaseRepository<long, ConsultantRoleEvaluation> evaluations,
        IConsultantRolePolicyProvider policies,
        IUnitOfWork unitOfWork)
    {
        this.profiles = profiles;
        this.reservations = reservations;
        this.evaluations = evaluations;
        this.policies = policies;
        this.unitOfWork = unitOfWork;
    }

    public async Task EvaluateDueConsultantsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueIds = await profiles.GetAll().AsNoTracking()
            .Where(x => !x.IsDeleted && x.User.IsActive &&
                        (x.NextRoleEvaluationAt == null || x.NextRoleEvaluationAt <= now))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in dueIds)
            await EvaluateAsync(id, now, cancellationToken);
    }

    public async Task<ConsultantRoleEvaluationStatus> GetStatusAsync(long consultantProfileId, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetAll().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == consultantProfileId && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("مشاور یافت نشد");
        var start = profile.RoleStartedAt ?? profile.CreatedAt;
        var next = profile.NextRoleEvaluationAt ?? start + policies.Get(profile.ConsultantRole).EvaluationPeriod;
        var now = DateTime.UtcNow;
        return new ConsultantRoleEvaluationStatus
        {
            CurrentRole = profile.ConsultantRole,
            PeriodStartedAt = start,
            NextEvaluationAt = next,
            SuccessfulPatientCount = await CountSuccessfulPatientsAsync(
                profile.Id,
                start,
                now < next ? now : next,
                cancellationToken),
            LastEvaluationResult = profile.LastEvaluationResult,
            LastEvaluatedAt = profile.LastEvaluatedAt
        };
    }

    private async Task EvaluateAsync(long id, DateTime now, CancellationToken cancellationToken)
    {
        var consultantLock = ConsultantLocks.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
        await consultantLock.WaitAsync(cancellationToken);
        try
        {
            await EvaluateInsideLockAsync(id, now, cancellationToken);
        }
        finally
        {
            consultantLock.Release();
        }
    }

    private async Task EvaluateInsideLockAsync(long id, DateTime now, CancellationToken cancellationToken)
    {
        DateTime? evaluatedPeriodStart = null;
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var profile = await profiles.GetAll().Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (profile is null || !profile.User.IsActive)
            {
                await unitOfWork.RollbackAsync();
                return;
            }

            var start = profile.RoleStartedAt ?? profile.CreatedAt;
            evaluatedPeriodStart = start;
            var policy = policies.Get(profile.ConsultantRole);
            var end = profile.NextRoleEvaluationAt ?? start + policy.EvaluationPeriod;
            if (now < end || await evaluations.GetAll().AnyAsync(
                    x => x.ConsultantProfileId == id && x.PeriodStartedAt == start, cancellationToken))
            {
                if (profile.NextRoleEvaluationAt is null)
                {
                    profile.RoleStartedAt = start;
                    profile.NextRoleEvaluationAt = end;
                    await unitOfWork.CommitAsync();
                }
                else
                    await unitOfWork.RollbackAsync();
                return;
            }

            var successful = await CountSuccessfulPatientsAsync(id, start, end, cancellationToken);
            var oldRole = profile.ConsultantRole;
            var decision = policies.Evaluate(oldRole, successful);

            await evaluations.AddAsync(new ConsultantRoleEvaluation
            {
                ConsultantProfileId = id,
                EvaluatedRole = oldRole,
                ResultingRole = decision.Deactivate ? null : decision.ResultingRole,
                PeriodStartedAt = start,
                PeriodEndedAt = end,
                EvaluatedAt = now,
                SuccessfulPatientCount = successful,
                Result = decision.Result,
                RewardLevel = decision.RewardLevel
            });

            if (decision.Deactivate)
            {
                profile.User.IsActive = false;
                profile.IsAvailable = false;
                profile.IsOnline = false;
                profile.NextRoleEvaluationAt = null;
            }
            else
            {
                profile.ConsultantRole = decision.ResultingRole;
                profile.RoleStartedAt = end;
                profile.NextRoleEvaluationAt = end + policies.Get(decision.ResultingRole).EvaluationPeriod;
            }

            profile.LastEvaluationResult = decision.Result;
            profile.LastEvaluatedAt = now;
            profile.UpdatedAt = now;
            await unitOfWork.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await unitOfWork.RollbackAsync();

            // Another application instance may have committed this exact period
            // after our initial check. The unique index is the final arbiter.
            if (evaluatedPeriodStart.HasValue &&
                await evaluations.GetAll().AsNoTracking().AnyAsync(
                    x => x.ConsultantProfileId == id && x.PeriodStartedAt == evaluatedPeriodStart.Value,
                    cancellationToken))
                return;

            throw;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private Task<int> CountSuccessfulPatientsAsync(long id, DateTime start, DateTime end, CancellationToken cancellationToken) =>
        SuccessfulPatientAttribution.CountAsync(reservations.GetAll(), id, start, end, cancellationToken);

}
