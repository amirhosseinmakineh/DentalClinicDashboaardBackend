using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
            var dentalServices = command.DentalServices.Distinct().ToList();
            if (dentalServices.Count == 0 || dentalServices.Any(x => !Enum.IsDefined(x)))
                return Result<CreateReservationResponse>.Failure("انتخاب حداقل یک خدمت معتبر الزامی است");

            if (!ReservationAppointmentTime.TryResolve(
                    command.ReservationAt,
                    command.AppointmentDateTime,
                    out var appointmentDateTime,
                    out var appointmentError))
                return Result<CreateReservationResponse>.Failure(appointmentError!);

            if (appointmentDateTime <= DateTime.Now)
                return Result<CreateReservationResponse>.Failure("زمان رزرو باید در آینده باشد");

            if (command.ReservationType == ReservationType.AfterSalesService &&
                string.IsNullOrWhiteSpace(command.Description))
                return Result<CreateReservationResponse>.Failure("توضیح نوع خدمت پس از فروش الزامی است");

            var consultantIsActive = await consultantProfileRepository.GetAll()
                .AnyAsync(x => x.Id == command.ConsultantProfileId &&
                               !x.IsDeleted && x.IsCompleteProfile &&
                               !x.User.IsDeleted && x.User.IsActive,
                    cancellationToken);
            if (!consultantIsActive)
                return Result<CreateReservationResponse>.Failure("مشاور فعال یافت نشد");

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
                return Result<CreateReservationResponse>.Failure("احتمال حضور باید بین ۰ تا 10 باشد");

            lead.PatientCity = patientCity;
            lead.PatientRegion = patientRegion;


            if (!string.IsNullOrWhiteSpace(command.SecondaryPhoneNumber))
                lead.SecondaryPhoneNumber = command.SecondaryPhoneNumber.Trim();

            if (command.AttendanceProbabilityPercent.HasValue)
                lead.AttendanceProbabilityPercent = command.AttendanceProbabilityPercent;

            leadAssignmentRepository.Update(lead);

            if (await reservationRepository.HasActiveReservationForLeadAsync(command.LeadAssignmentId))
                return Result<CreateReservationResponse>.Failure("برای این بیمار قبلا رزرو فعال ثبت شده است");

            var sameTimeCount = await reservationRepository.CountActiveReservationsAtAsync(command.ConsultantProfileId, appointmentDateTime);
            if (sameTimeCount >= MaxReservationsPerConsultantAtSameTime)
                return Result<CreateReservationResponse>.Failure("ظرفیت این بازه زمانی برای مشاور تکمیل است");

            var reservation = new Domain.Models.Reservation
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = command.ConsultantProfileId,
                OwnerType = command.OwnerType,
                OwnerUserId = command.OwnerUserId,
                ReservationAt = appointmentDateTime,
                ReservationType = command.ReservationType,
                DentalServices = dentalServices,
                AttendanceConfirmationStatus = ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation,
                Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
                AttendancePrediction = string.IsNullOrWhiteSpace(command.AttendancePrediction)
                    ? null
                    : command.AttendancePrediction.Trim(),
                CreatedAt = DateTime.UtcNow,
                InitialReservationAt = appointmentDateTime,
                LastActivityAt = DateTime.UtcNow,
            };

            await reservationRepository.AddAsync(reservation);
            await reservationRepository.SaveChange();

            return Result<CreateReservationResponse>.Success(new CreateReservationResponse
            {
                Id = reservation.Id,
                ReservationId = reservation.Id,
                LeadAssignmentId = reservation.LeadAssignmentId,
                ConsultantProfileId = reservation.ConsultantProfileId,
                PatientUserId = reservation.PatientUserId,
                RequiresPatientProfile = !reservation.PatientUserId.HasValue,
                ReservationAt = reservation.ReservationAt,
                AppointmentDateTime = reservation.ReservationAt,
                CreatedAt = reservation.CreatedAt,
                ReservationType = reservation.ReservationType,
                SecondaryPhoneNumber = lead.SecondaryPhoneNumber,
                PatientCity = lead.PatientCity ?? string.Empty,
                PatientRegion = lead.PatientRegion,
                BusinessName = lead.BusinessName,
                AttendanceProbabilityPercent = lead.AttendanceProbabilityPercent,
                AttendancePrediction = reservation.AttendancePrediction,
                AttendanceConfirmationStatus = reservation.AttendanceConfirmationStatus,
                PatientName = lead.UserName,
                PatientPhoneNumber = lead.PhoneNumber,
                DentalServices = reservation.DentalServices,


            }, "رزرو با موفقیت ثبت شد");
        }
    }
}
