using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class CompleteReservationPatientProfileCommand : ICommand<CompleteReservationPatientProfileResponse>
    {
        public long ReservationId { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string? AvatarImageName { get; set; }
        public Gender Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string NationalCode { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string? EmergencyPhoneNumber { get; set; }
        public string? InsuranceName { get; set; }
        public string? Notes { get; set; }
    }
}
