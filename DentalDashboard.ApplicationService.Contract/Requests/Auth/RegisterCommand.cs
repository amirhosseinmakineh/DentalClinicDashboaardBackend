using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Requests.Auth
{
    public class RegisterCommand : ICommand<object>
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
