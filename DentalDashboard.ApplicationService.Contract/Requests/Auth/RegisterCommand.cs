using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Auth
{
    public class RegisterCommand : ICommand<RegisterResponse>
    {
        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string PasswordHash { get; set; } = default!;

        public bool IsCompleteProfile { get; set; }

        public string? AvatarImageName { get; set; }

        public Gender Gender { get; set; }

        public DateTime BirthDate { get; set; }
    }
}
