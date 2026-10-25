using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;

public sealed class UpdateReservationDoctorRequest
{
    public string? DoctorName { get; set; }
}

public sealed class UpdateReservationDoctorCommand
    : ICommand<UpdateReservationDoctorResponse>
{
    public long ReservationId { get; set; }
    public string? DoctorName { get; set; }
}
