using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser
{
    public class UpdateUserCommand : ICommand
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;
        public bool IsCompleteProfile { get; set; }

        public string? AvatarImageName { get; set; }

        public Gender Gender { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
