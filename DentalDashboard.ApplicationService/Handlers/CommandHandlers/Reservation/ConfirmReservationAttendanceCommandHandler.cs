using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class ConfirmReservationAttendanceCommandHandler : ICommandHandler<ConfirmReservationAttendanceCommand>
    {
        private readonly IReservationRepository reservationRepository;

        public ConfirmReservationAttendanceCommandHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<Result> HandleAsync(ConfirmReservationAttendanceCommand command, CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == command.ReservationId && x.ConsultantProfileId == command.ConsultantProfileId, cancellationToken);

            if (reservation == null || reservation.IsDeleted)
                return Result.Failure("رزرو برای این مشاور یافت نشد");

            if (reservation.IsCanceled)
                return Result.Failure("رزرو لغو شده قابل تایید حضور نیست");

            if (reservation.ReservationAt > DateTime.Now)
                return Result.Failure("تایید حضور فقط بعد از رسیدن روز و ساعت رزرو ممکن است");

            if (reservation.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved ||
                reservation.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryRejected)
                return Result.Failure("این رزرو قبلا توسط منشی بررسی شده است");

            if (reservation.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent ||
                reservation.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent)
                return Result.Failure("تایید حضور این رزرو قبلا ثبت شده است");

            reservation.ConsultantSaysPatientAttended = command.PatientAttended;
            reservation.ConsultantAttendanceConfirmedAt = DateTime.UtcNow;
            reservation.ConsultantAttendanceNote = command.Note;
            reservation.AttendanceConfirmationStatus = command.PatientAttended
                ? ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent
                : ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent;
            reservation.UpdatedAt = DateTime.UtcNow;

            reservationRepository.Update(reservation);
            await reservationRepository.SaveChange();

            return Result.Success("تایید حضور بیمار ثبت شد و در انتظار بررسی منشی است");
        }
    }
}
