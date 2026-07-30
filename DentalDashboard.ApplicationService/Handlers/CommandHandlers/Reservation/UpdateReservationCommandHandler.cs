using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class UpdateReservationCommandHandler : ICommandHandler<UpdateReservationCommand, ReservationItemResponse>
    {
        private const int MaxReservationsPerConsultantAtSameTime = 10;
        private readonly IReservationRepository reservationRepository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public UpdateReservationCommandHandler(
            IReservationRepository reservationRepository,
            ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.reservationRepository = reservationRepository;
            this.leadAssignmentRepository = leadAssignmentRepository;
        }

        public async Task<Result<ReservationItemResponse>> HandleAsync(
            UpdateReservationCommand command,
            CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetByIdAsync(command.ReservationId);
            if (reservation == null || reservation.IsDeleted || reservation.IsCanceled)
                return Result<ReservationItemResponse>.Failure("رزرو فعال یافت نشد");

            if (reservation.ConsultantProfileId != command.ConsultantProfileId)
                return Result<ReservationItemResponse>.Failure("این رزرو متعلق به شما نیست");

            if (reservation.AttendanceConfirmationStatus is
                ReservationAttendanceConfirmationStatus.SecretaryApproved or
                ReservationAttendanceConfirmationStatus.SecretaryRejected)
            {
                return Result<ReservationItemResponse>.Failure("پس از بررسی منشی امکان ویرایش رزرو وجود ندارد");
            }

            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(
                reservation.LeadAssignmentId,
                command.ConsultantProfileId);
            if (lead == null || lead.IsDeleted)
                return Result<ReservationItemResponse>.Failure("لید مرتبط با رزرو یافت نشد");

            var patientCity = !string.IsNullOrWhiteSpace(command.PatientCity)
                ? command.PatientCity.Trim()
                : lead.PatientCity?.Trim();
            var patientRegion = !string.IsNullOrWhiteSpace(command.PatientRegion)
                ? command.PatientRegion.Trim()
                : lead.PatientRegion?.Trim();

            if (string.IsNullOrWhiteSpace(patientCity))
                return Result<ReservationItemResponse>.Failure("شهر بیمار برای رزرو الزامی است");

            if (string.IsNullOrWhiteSpace(patientRegion))
                return Result<ReservationItemResponse>.Failure("منطقه بیمار برای رزرو الزامی است");

            if (command.AttendanceProbabilityPercent.HasValue &&
                (command.AttendanceProbabilityPercent < 0 || command.AttendanceProbabilityPercent > 100))
            {
                return Result<ReservationItemResponse>.Failure("احتمال حضور باید بین ۰ تا ۱۰۰ باشد");
            }

            var reservationTimeChanged = reservation.ReservationAt != command.ReservationAt;
            if (reservationTimeChanged && command.ReservationAt <= DateTime.Now)
                return Result<ReservationItemResponse>.Failure("زمان رزرو باید در آینده باشد");

            if (reservationTimeChanged)
            {
                var sameTimeCount = await reservationRepository.CountActiveReservationsAtExcludingAsync(
                    command.ConsultantProfileId,
                    command.ReservationAt,
                    reservation.Id);
                if (sameTimeCount >= MaxReservationsPerConsultantAtSameTime)
                {
                    return Result<ReservationItemResponse>.Failure("ظرفیت این بازه زمانی برای مشاور تکمیل است");
                }
            }

            lead.PatientCity = patientCity;
            lead.PatientRegion = patientRegion;

            if (!string.IsNullOrWhiteSpace(command.SecondaryPhoneNumber))
                lead.SecondaryPhoneNumber = command.SecondaryPhoneNumber.Trim();

            if (command.AttendanceProbabilityPercent.HasValue)
                lead.AttendanceProbabilityPercent = command.AttendanceProbabilityPercent;

            leadAssignmentRepository.Update(lead);

            reservation.ReservationAt = command.ReservationAt;
            reservation.Description = command.Description?.Trim();
            reservation.AttendancePrediction = string.IsNullOrWhiteSpace(command.AttendancePrediction)
                ? null
                : command.AttendancePrediction.Trim();
            reservation.UpdatedAt = DateTime.UtcNow;

            reservationRepository.Update(reservation);
            await reservationRepository.SaveChange();

            return Result<ReservationItemResponse>.Success(new ReservationItemResponse
            {
                Id = reservation.Id,
                LeadAssignmentId = reservation.LeadAssignmentId,
                ConsultantProfileId = reservation.ConsultantProfileId,
                PatientUserId = reservation.PatientUserId,
                RequiresPatientProfile = !reservation.PatientUserId.HasValue,
                ReservationAt = reservation.ReservationAt,
                PatientName = lead.UserName,
                PatientPhoneNumber = lead.PhoneNumber,
                SecondaryPhoneNumber = lead.SecondaryPhoneNumber,
                PatientCity = lead.PatientCity ?? string.Empty,
                PatientRegion = lead.PatientRegion,
                BusinessName = lead.BusinessName,
                AttendanceProbabilityPercent = lead.AttendanceProbabilityPercent,
                AttendancePrediction = reservation.AttendancePrediction,
                AttendanceConfirmationStatus = reservation.AttendanceConfirmationStatus,
                ConsultantAttendanceConfirmedAt = reservation.ConsultantAttendanceConfirmedAt,
                ConsultantSaysPatientAttended = reservation.ConsultantSaysPatientAttended,
                ConsultantAttendanceNote = reservation.ConsultantAttendanceNote,
                SecretaryReviewedAt = reservation.SecretaryReviewedAt,
                SecretaryUserId = reservation.SecretaryUserId,
                SecretaryApprovedConsultantConfirmation = reservation.SecretaryApprovedConsultantConfirmation,
                SecretaryReviewNote = reservation.SecretaryReviewNote,
                IsAttendanceScoreApplied = reservation.IsAttendanceScoreApplied,
                AttendanceScoreValue = reservation.AttendanceScoreValue,
                AttendanceScoreAppliedAt = reservation.AttendanceScoreAppliedAt,
                IsDueForConsultantConfirmation =
                    reservation.ReservationAt <= DateTime.Now &&
                    reservation.AttendanceConfirmationStatus ==
                    ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation,
                Description = reservation.Description,
                IsCanceled = reservation.IsCanceled
            }, "رزرو با موفقیت ویرایش شد");
        }
    }
}
