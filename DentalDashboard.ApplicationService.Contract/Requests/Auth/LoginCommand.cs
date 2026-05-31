using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Auth
{
    public class LoginCommand : ICommand<object>
    {
        public Guid UserId { get; set; }
        public string PhoneNumber { get; set; } = default!;

        public string PasswordHash { get; set; } = default!;
    }
}
