using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Strategies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public sealed class EvaluateTopSellerConsultantsCommandHandler :
    ICommandHandler<EvaluateTopSellerConsultantsCommand>
{
    private readonly IConsultantProfileRepository consultants;
    private readonly IReservationRepository reservations;
    private readonly IConsultantProfileService profileService;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<EvaluateTopSellerConsultantsCommandHandler> logger;
    private readonly TopSellerDistributionStrategy strategy = new(TopSellerPolicy.Default);

    public EvaluateTopSellerConsultantsCommandHandler(IConsultantProfileRepository consultants,
        IReservationRepository reservations, IConsultantProfileService profileService,
        IUnitOfWork unitOfWork, ILogger<EvaluateTopSellerConsultantsCommandHandler> logger)
    {
        this.consultants = consultants; this.reservations = reservations;
        this.profileService = profileService; this.unitOfWork = unitOfWork; this.logger = logger;
    }

    public async Task<Result> HandleAsync(EvaluateTopSellerConsultantsCommand command,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var nowIran = IranTimeHelper.ToIranLocalTime(nowUtc);
        foreach (var consultant in await consultants
                     .GetTopSellerConsultantsReadyForEvaluationAsync(nowUtc.AddDays(-7)))
        {
            var periodStart = consultant.TopSellerStartedAt!.Value;
            var periodEnd = periodStart.AddDays(7);
            logger.LogInformation(
                "TopSeller weekly evaluation started for {ConsultantId}, window {PeriodStart} - {PeriodEnd}",
                consultant.Id, periodStart, periodEnd);
            var successful = await reservations.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsCanceled && x.ConsultantProfileId == consultant.Id &&
                            x.LeadAssignment.AssignedAt >= periodStart &&
                            x.LeadAssignment.AssignedAt < periodEnd &&
                            x.ConsultantSaysPatientAttended == true &&
                            x.SecretaryApprovedConsultantConfirmation == true &&
                            x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved)
                .Select(x => x.LeadAssignmentId).Distinct().CountAsync(cancellationToken);
            var decision = strategy.Decide(new TopSellerContext
            {
                TopSellerStartedAt = IranTimeHelper.ToIranLocalTime(periodStart),
                CurrentTime = nowIran, SuccessfulPatients = successful,
                IsActive = consultant.User.IsActive, IsAvailable = consultant.IsAvailable,
                IsOnline = consultant.IsOnline
            });
            if (!decision.IsReadyForWeeklyEvaluation)
                continue;
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!await consultants.TryCompleteTopSellerEvaluationAsync(
                        consultant.Id, periodStart, nowUtc, periodEnd, decision.RewardLevel))
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None);
                    continue;
                }

                if (decision.ShouldDowngradeToSeller)
                {
                    await profileService.SetConsultantLevelAsync(consultant.UserId, ConsultantLevel.Seller);
                    logger.LogWarning("TopSeller {ConsultantId} downgraded to Seller with {SuccessfulPatients} patients",
                        consultant.Id, successful);
                }
                else
                {
                    logger.LogInformation(
                        "TopSeller {ConsultantId} remained TopSeller with {SuccessfulPatients} patients and reward {RewardLevel}",
                        consultant.Id, successful, decision.RewardLevel);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        return Result.Success("TopSeller weekly evaluation cycle completed.");
    }
}
