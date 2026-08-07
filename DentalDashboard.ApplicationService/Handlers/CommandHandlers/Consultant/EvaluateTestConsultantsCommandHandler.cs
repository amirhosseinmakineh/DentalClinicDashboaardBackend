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

public sealed class EvaluateTestConsultantsCommandHandler : ICommandHandler<EvaluateTestConsultantsCommand>
{
    private readonly IConsultantProfileRepository consultantRepository;
    private readonly ILeadAssignmentRepository leadRepository;
    private readonly IReservationRepository reservationRepository;
    private readonly IConsultantProfileService consultantProfileService;
    private readonly ILogger<EvaluateTestConsultantsCommandHandler> logger;
    private readonly TestConsultantStrategy strategy = new(TestConsultantPolicy.Default);

    public EvaluateTestConsultantsCommandHandler(
        IConsultantProfileRepository consultantRepository,
        ILeadAssignmentRepository leadRepository,
        IReservationRepository reservationRepository,
        IConsultantProfileService consultantProfileService,
        ILogger<EvaluateTestConsultantsCommandHandler> logger)
    {
        this.consultantRepository = consultantRepository;
        this.leadRepository = leadRepository;
        this.reservationRepository = reservationRepository;
        this.consultantProfileService = consultantProfileService;
        this.logger = logger;
    }

    public async Task<Result> HandleAsync(EvaluateTestConsultantsCommand command, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var nowIran = IranTimeHelper.ToIranLocalTime(nowUtc);
        var candidates = await consultantRepository.GetTestConsultantsReadyForEvaluationAsync(nowUtc.AddDays(-10));

        foreach (var consultant in candidates)
        {
            var periodEnd = consultant.TestStartedAt!.Value.AddDays(10);
            var confirmedPatients = await reservationRepository.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsCanceled && x.ConsultantProfileId == consultant.Id &&
                            x.LeadAssignment.AssignedAt >= consultant.TestStartedAt &&
                            x.LeadAssignment.AssignedAt < periodEnd &&
                            x.ConsultantSaysPatientAttended == true &&
                            x.SecretaryApprovedConsultantConfirmation == true &&
                            x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved)
                .Select(x => x.LeadAssignmentId).Distinct().CountAsync(cancellationToken);
            var assignedToday = await leadRepository.GetTodayAssignmentCountAsync(consultant.Id, burned: true, cancellationToken);
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
                await consultantProfileService.DeactivateConsultantAsync(consultant.Id);
                logger.LogWarning(
                    "TEST consultant {ConsultantId} failed evaluation and account/dashboard was disabled",
                    consultant.Id);
            }

            consultantRepository.Update(consultant);
            await consultantRepository.SaveChange();

            if (decision.HasPassed)
                await consultantProfileService.SetConsultantLevelAsync(consultant.UserId, ConsultantLevel.Seller);
        }

        return Result.Success("TEST consultant evaluation cycle completed.");
    }
}
