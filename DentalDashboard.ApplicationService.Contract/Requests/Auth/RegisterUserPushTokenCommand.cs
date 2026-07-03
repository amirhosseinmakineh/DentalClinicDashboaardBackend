using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Auth
{
    public class RegisterUserPushTokenCommand : ICommand
    {
        public Guid UserId { get; set; }

        public string DeviceToken { get; set; } = default!;
    }
}
