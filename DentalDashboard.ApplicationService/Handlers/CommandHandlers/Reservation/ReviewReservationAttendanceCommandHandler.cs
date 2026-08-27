using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class ReviewReservationAttendanceCommandHandler : ICommandHandler<ReviewReservationAttendanceCommand>
    {
        private readonly IReservationRepository reservationRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public ReviewReservationAttendanceCommandHandler(
            IReservationRepository reservationRepository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.reservationRepository = reservationRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result> HandleAsync(ReviewReservationAttendanceCommand command, CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetAll().FirstOrDefaultAsync(x => x.Id == command.ReservationId, cancellationToken);
            if (reservation == null || reservation.IsDeleted)
                return Result.Failure("رزرو یافت نشد");

            if (reservation.IsCanceled)
                return Result.Failure("رزرو لغو شده قابل بررسی نیست");

            if (reservation.ReservationAt > DateTime.Now)
                return Result.Failure("نتیجه خدمت فقط بعد از زمان مراجعه قابل ثبت است");

            if (reservation.IsAttendanceScoreApplied)
                return Result.Failure("بررسی این رزرو قبلا ثبت شده است");

            var patientReceivedService = command.PatientReceivedService ?? command.Approved;
            if (!patientReceivedService.HasValue)
                return Result.Failure("وضعیت انجام یا عدم انجام خدمت باید مشخص شود");

            var profile = await consultantProfileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == reservation.ConsultantProfileId, cancellationToken);
            if (profile == null || profile.IsDeleted)
                return Result.Failure("پروفایل مشاور یافت نشد");

            reservation.SecretaryUserId = command.SecretaryUserId;
            reservation.PatientReceivedService = patientReceivedService.Value;
            reservation.SecretaryApprovedConsultantConfirmation = patientReceivedService.Value;
            reservation.SecretaryReviewedAt = DateTime.UtcNow;
            reservation.SecretaryReviewNote = command.Note;
            reservation.AttendanceConfirmationStatus = patientReceivedService.Value
                ? ReservationAttendanceConfirmationStatus.SecretaryApproved
                : ReservationAttendanceConfirmationStatus.SecretaryRejected;
            reservation.IsAttendanceScoreApplied = true;
            reservation.AttendanceScoreValue = null;
            reservation.AttendanceScoreAppliedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;
            reservation.DoctorName = command.DoctorName;

            reservationRepository.Update(reservation);
            await reservationRepository.SaveChange();

            return Result.Success(patientReceivedService.Value
                ? "انجام خدمت برای بیمار تایید شد"
                : "عدم انجام خدمت برای بیمار ثبت و رزرو رد شد");
        }
    }
}
