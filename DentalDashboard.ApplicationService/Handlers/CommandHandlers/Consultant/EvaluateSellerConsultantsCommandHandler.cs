using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Strategies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public sealed class EvaluateSellerConsultantsCommandHandler : ICommandHandler<EvaluateSellerConsultantsCommand>
{
    private readonly IConsultantProfileRepository consultants;
    private readonly IReservationRepository reservations;
    private readonly IConsultantProfileService profileService;
    private readonly SellerDistributionStrategy strategy = new(SellerConsultantPolicy.Default);

    public EvaluateSellerConsultantsCommandHandler(IConsultantProfileRepository consultants,
        IReservationRepository reservations, IConsultantProfileService profileService)
    {
        this.consultants = consultants; this.reservations = reservations; this.profileService = profileService;
    }

    public async Task<Result> HandleAsync(EvaluateSellerConsultantsCommand command,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var nowIran = IranTimeHelper.ToIranLocalTime(nowUtc);
        foreach (var seller in await consultants.GetSellerConsultantsReadyForEvaluationAsync(nowUtc.AddDays(-10)))
        {
            var periodEnd = seller.SellerStartedAt!.Value.AddDays(10);
            var confirmed = await reservations.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsCanceled && x.ConsultantProfileId == seller.Id &&
                            x.LeadAssignment.AssignedAt >= seller.SellerStartedAt &&
                            x.LeadAssignment.AssignedAt < periodEnd &&
                            x.ConsultantSaysPatientAttended == true &&
                            x.SecretaryApprovedConsultantConfirmation == true &&
                            x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved)
                .Select(x => x.LeadAssignmentId).Distinct().CountAsync(cancellationToken);
            var decision = strategy.Decide(new SellerConsultantContext
            {
                SellerStartedAt = IranTimeHelper.ToIranLocalTime(seller.SellerStartedAt.Value),
                CurrentTime = nowIran, ConfirmedPatientCount = confirmed,
                IsActive = seller.User.IsActive, IsAvailable = seller.IsAvailable, IsOnline = seller.IsOnline
            });
            if (!decision.IsReadyForEvaluation ||
                !await consultants.TryCompleteSellerEvaluationAsync(seller.Id, nowUtc))
                continue;

            var target = decision.ShouldPromoteToGold ? ConsultantLevel.TopSeller
                : decision.ShouldReturnToTest ? ConsultantLevel.Test : ConsultantLevel.Seller;
            await profileService.SetConsultantLevelAsync(seller.UserId, target);
        }
        return Result.Success("Seller evaluation cycle completed.");
    }
}
