using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands
{
    public class RegisterPushTokenCommand : ICommand
    {
        public long ProfileId { get; set; }
        public string DeviceToken { get; set; } = default!;
    }
}
