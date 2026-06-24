using DentalDashboard.ApplicationService.Contract.Responses.PatientResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Patient.Commands
{
    public class CompletePatientProfileFromReservationCommand : ICommand<CompletePatientProfileFromReservationResponse>
    {
        public long ReservationId { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
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
