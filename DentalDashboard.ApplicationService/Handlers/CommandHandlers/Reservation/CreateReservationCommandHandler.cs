using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class CreateReservationCommandHandler : ICommandHandler<CreateReservationCommand, CreateReservationResponse>
    {
        private const int MaxReservationsPerConsultantAtSameTime = 10;
        private readonly IReservationRepository reservationRepository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public CreateReservationCommandHandler(IReservationRepository reservationRepository, ILeadAssignmentRepository leadAssignmentRepository, IConsultantProfileRepository consultantProfileRepository)
        {
            this.reservationRepository = reservationRepository;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result<CreateReservationResponse>> HandleAsync(CreateReservationCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ReservationAt <= DateTime.Now)
                return Result<CreateReservationResponse>.Failure("زمان رزرو باید در آینده باشد");

            var consultant = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (consultant == null || consultant.IsDeleted)
                return Result<CreateReservationResponse>.Failure("مشاور یافت نشد");

            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(command.LeadAssignmentId, command.ConsultantProfileId);
            if (lead == null || lead.IsDeleted)
                return Result<CreateReservationResponse>.Failure("لید برای این مشاور یافت نشد");

            if (lead.ReportSubmittedAt == null || (lead.CallResult != LeadCallResult.Contacted && lead.CallResult != LeadCallResult.Converted))
                return Result<CreateReservationResponse>.Failure("فقط لیدهای تماس موفق قابل رزرو هستند");

            var patientCity = !string.IsNullOrWhiteSpace(command.PatientCity)
                ? command.PatientCity.Trim()
                : lead.PatientCity?.Trim();
            var patientRegion = !string.IsNullOrWhiteSpace(command.PatientRegion)
                ? command.PatientRegion.Trim()
                : lead.PatientRegion?.Trim();

            if (string.IsNullOrWhiteSpace(patientCity))
                return Result<CreateReservationResponse>.Failure("شهر بیمار برای رزرو الزامی است");

            if (string.IsNullOrWhiteSpace(patientRegion))
                return Result<CreateReservationResponse>.Failure("منطقه بیمار برای رزرو الزامی است");

            if (command.AttendanceProbabilityPercent.HasValue &&
                (command.AttendanceProbabilityPercent < 0 || command.AttendanceProbabilityPercent > 100))
                return Result<CreateReservationResponse>.Failure("احتمال حضور باید بین ۰ تا ۱۰۰ باشد");

            lead.PatientCity = patientCity;
            lead.PatientRegion = patientRegion;

            if (!string.IsNullOrWhiteSpace(command.SecondaryPhoneNumber))
                lead.SecondaryPhoneNumber = command.SecondaryPhoneNumber.Trim();

            if (command.AttendanceProbabilityPercent.HasValue)
                lead.AttendanceProbabilityPercent = command.AttendanceProbabilityPercent;

            leadAssignmentRepository.Update(lead);

            if (await reservationRepository.HasActiveReservationForLeadAsync(command.LeadAssignmentId))
                return Result<CreateReservationResponse>.Failure("برای این بیمار قبلا رزرو فعال ثبت شده است");

            var sameTimeCount = await reservationRepository.CountActiveReservationsAtAsync(command.ConsultantProfileId, command.ReservationAt);
            if (sameTimeCount >= MaxReservationsPerConsultantAtSameTime)
                return Result<CreateReservationResponse>.Failure("ظرفیت این بازه زمانی برای مشاور تکمیل است");

            var reservation = new Domain.Models.Reservation
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = command.ConsultantProfileId,
                ReservationAt = command.ReservationAt,
                AttendanceConfirmationStatus = ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation,
                Description = command.Description,
                CreatedAt = DateTime.UtcNow
            };

            await reservationRepository.AddAsync(reservation);
            await reservationRepository.SaveChange();

            return Result<CreateReservationResponse>.Success(new CreateReservationResponse
            {
                Id = reservation.Id,
                LeadAssignmentId = reservation.LeadAssignmentId,
                ConsultantProfileId = reservation.ConsultantProfileId,
                PatientUserId = reservation.PatientUserId,
                RequiresPatientProfile = !reservation.PatientUserId.HasValue,
                ReservationAt = reservation.ReservationAt,
                SecondaryPhoneNumber = lead.SecondaryPhoneNumber,
                PatientCity = lead.PatientCity ?? string.Empty,
                PatientRegion = lead.PatientRegion,
                BusinessName = lead.BusinessName,
                AttendanceProbabilityPercent = lead.AttendanceProbabilityPercent,
                AttendanceConfirmationStatus = reservation.AttendanceConfirmationStatus,
                PatientName = lead.UserName,
                PatientPhoneNumber = lead.PhoneNumber
            }, "رزرو با موفقیت ثبت شد");
        }
    }
}
