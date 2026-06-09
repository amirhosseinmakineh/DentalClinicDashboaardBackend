using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands
{
    public class SetOnlineOfflineCommand : ICommand
    {
        public long ProfileId { get; set; }
        public bool IsOnline { get; set; }
        public bool IsOffline { get; set; }
    }
}
