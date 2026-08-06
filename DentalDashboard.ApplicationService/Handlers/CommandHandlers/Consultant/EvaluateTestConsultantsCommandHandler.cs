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

public sealed class EvaluateTestConsultantsCommandHandler : ICommandHandler<EvaluateTestConsultantsCommand>
{
    private readonly IConsultantProfileRepository consultantRepository;
    private readonly ILeadAssignmentRepository leadRepository;
    private readonly IReservationRepository reservationRepository;
    private readonly IConsultantProfileService consultantProfileService;
    private readonly TestConsultantStrategy strategy = new(TestConsultantPolicy.Default);

    public EvaluateTestConsultantsCommandHandler(
        IConsultantProfileRepository consultantRepository,
        ILeadAssignmentRepository leadRepository,
        IReservationRepository reservationRepository,
        IConsultantProfileService consultantProfileService)
    {
        this.consultantRepository = consultantRepository;
        this.leadRepository = leadRepository;
        this.reservationRepository = reservationRepository;
        this.consultantProfileService = consultantProfileService;
    }

    public async Task<Result> HandleAsync(EvaluateTestConsultantsCommand command, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var nowIran = IranTimeHelper.ToIranLocalTime(nowUtc);
        var candidates = await consultantRepository.GetTestConsultantsReadyForEvaluationAsync(nowUtc.AddDays(-10));

        foreach (var consultant in candidates)
        {
            var confirmedPatients = await reservationRepository.GetAll().AsNoTracking().CountAsync(
                x => !x.IsDeleted && !x.IsCanceled && x.ConsultantProfileId == consultant.Id &&
                     x.LeadAssignment.AssignedAt >= consultant.TestStartedAt &&
                     x.ConsultantSaysPatientAttended == true &&
                     x.SecretaryApprovedConsultantConfirmation == true &&
                     x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved,
                cancellationToken);
            var assignedToday = await leadRepository.GetTodayPickupCountAsync(consultant.Id);
            var decision = strategy.Decide(new TestConsultantContext
            {
                TestStartedAt = IranTimeHelper.ToIranLocalTime(consultant.TestStartedAt!.Value),
                CurrentTime = nowIran,
                AssignedTodayCount = assignedToday,
                ConfirmedPatientCount = confirmedPatients,
                IsActive = consultant.User.IsActive,
                IsAvailable = consultant.IsAvailable,
                IsOnline = consultant.IsOnline
            });

            if (!decision.IsReadyForEvaluation || consultant.TestCompletedAt.HasValue)
                continue;

            consultant.TestCompletedAt = nowUtc;
            consultant.TestPassed = decision.HasPassed;
            if (!decision.HasPassed)
            {
                consultant.IsAvailable = false;
                consultant.IsOnline = false;
                consultant.User.IsActive = false;
            }

            consultantRepository.Update(consultant);
            await consultantRepository.SaveChange();

            if (decision.HasPassed)
                await consultantProfileService.SetConsultantLevelAsync(consultant.UserId, ConsultantLevel.Seller);
        }

        return Result.Success("TEST consultant evaluation cycle completed.");
    }
}
