using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class SendTestPushNotificationCommand : ICommand
{
    public long ProfileId { get; set; }
}
