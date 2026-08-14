using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.RolePolicies;
using DentalDashboard.Framwork.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services;

public sealed class ConsultantRoleEvaluationService : IConsultantRoleEvaluationService
{
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
        return new ConsultantRoleEvaluationStatus
        {
            CurrentRole = profile.ConsultantRole,
            PeriodStartedAt = start,
            NextEvaluationAt = next,
            SuccessfulPatientCount = await CountSuccessfulPatientsAsync(profile.Id, start, DateTime.UtcNow, cancellationToken),
            LastEvaluationResult = profile.LastEvaluationResult,
            LastEvaluatedAt = profile.LastEvaluatedAt
        };
    }

    private async Task EvaluateAsync(long id, DateTime now, CancellationToken cancellationToken)
    {
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
            var (newRole, result, reward) = Decide(oldRole, successful, policy);

            await evaluations.AddAsync(new ConsultantRoleEvaluation
            {
                ConsultantProfileId = id,
                EvaluatedRole = oldRole,
                ResultingRole = result == ConsultantEvaluationResult.Deactivated ? null : newRole,
                PeriodStartedAt = start,
                PeriodEndedAt = end,
                EvaluatedAt = now,
                SuccessfulPatientCount = successful,
                Result = result,
                RewardLevel = reward
            });

            if (result == ConsultantEvaluationResult.Deactivated)
            {
                profile.User.IsActive = false;
                profile.IsAvailable = false;
                profile.IsOnline = false;
                profile.NextRoleEvaluationAt = null;
            }
            else
            {
                profile.ConsultantRole = newRole;
                profile.RoleStartedAt = now;
                profile.NextRoleEvaluationAt = now + policies.Get(newRole).EvaluationPeriod;
            }

            profile.LastEvaluationResult = result;
            profile.LastEvaluatedAt = now;
            profile.UpdatedAt = now;
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private Task<int> CountSuccessfulPatientsAsync(long id, DateTime start, DateTime end, CancellationToken cancellationToken) =>
        reservations.GetAll().AsNoTracking()
            .Where(x => x.ConsultantProfileId == id && !x.IsDeleted && !x.IsCanceled && x.PatientUserId.HasValue &&
                        x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved &&
                        x.SecretaryReviewedAt >= start && x.SecretaryReviewedAt < end)
            .Select(x => x.PatientUserId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

    private static (ConsultantRole Role, ConsultantEvaluationResult Result, int Reward) Decide(
        ConsultantRole role, int successful, ConsultantRolePolicy policy) => role switch
        {
            ConsultantRole.Test when successful >= policy.PromotionThreshold =>
                (ConsultantRole.Seller, ConsultantEvaluationResult.PromotedToSeller, 0),
            ConsultantRole.Test => (ConsultantRole.Test, ConsultantEvaluationResult.Deactivated, 0),
            ConsultantRole.Seller when successful >= policy.PromotionThreshold =>
                (ConsultantRole.TopSeller, ConsultantEvaluationResult.PromotedToTopSeller, 0),
            ConsultantRole.Seller when successful < policy.DemotionThreshold =>
                (ConsultantRole.Test, ConsultantEvaluationResult.DemotedToTest, 0),
            ConsultantRole.Seller => (ConsultantRole.Seller, ConsultantEvaluationResult.RemainedSeller, 0),
            ConsultantRole.TopSeller when successful < policy.DemotionThreshold =>
                (ConsultantRole.Seller, ConsultantEvaluationResult.DemotedToSeller, 0),
            ConsultantRole.TopSeller when successful >= policy.HigherRewardThreshold =>
                (ConsultantRole.TopSeller, ConsultantEvaluationResult.TopSellerHigherReward, 2),
            ConsultantRole.TopSeller when successful >= policy.RewardThreshold =>
                (ConsultantRole.TopSeller, ConsultantEvaluationResult.TopSellerReward, 1),
            _ => (ConsultantRole.TopSeller, ConsultantEvaluationResult.RemainedTopSeller, 0)
        };
}
