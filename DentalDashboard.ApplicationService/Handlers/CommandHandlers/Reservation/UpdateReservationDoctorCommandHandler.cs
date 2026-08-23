using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;

public sealed class UpdateReservationDoctorCommandHandler(IReservationRepository reservations)
    : ICommandHandler<UpdateReservationDoctorCommand, UpdateReservationDoctorResponse>
{
    public async Task<Result<UpdateReservationDoctorResponse>> HandleAsync(
        UpdateReservationDoctorCommand command,
        CancellationToken cancellationToken = default)
    {
        var doctorName = command.DoctorName?.Trim();

        if (string.IsNullOrWhiteSpace(doctorName))
        {
            return Result<UpdateReservationDoctorResponse>.Failure("نام دکتر الزامی است");
        }

        if (doctorName.Length > 150)
        {
            return Result<UpdateReservationDoctorResponse>.Failure("نام دکتر نباید بیشتر از ۱۵۰ کاراکتر باشد");
        }

        var reservation = await reservations.GetAll()
            .FirstOrDefaultAsync(
                item => item.Id == command.ReservationId && !item.IsDeleted,
                cancellationToken);

        if (reservation == null)
        {
            return Result<UpdateReservationDoctorResponse>.Failure("رزرو یافت نشد");
        }

        reservation.DoctorName = doctorName;
        reservation.UpdatedAt = DateTime.UtcNow;
        reservations.Update(reservation);
        await reservations.SaveChange();

        return Result<UpdateReservationDoctorResponse>.Success(
            new UpdateReservationDoctorResponse
            {
                ReservationId = reservation.Id,
                DoctorName = doctorName
            },
            "دکتر با موفقیت به بیمار تخصیص داده شد.");
    }
}
