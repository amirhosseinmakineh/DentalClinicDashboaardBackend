using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant
{
    public class SetAvailableCommand : ICommand
    {
        public long ProfileId { get; set; }
        public bool IsAvailable { get; set; }
    }
}
