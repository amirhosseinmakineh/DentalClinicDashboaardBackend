using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class RejectBroadcastCommand : ICommand
{
    public long LeadAssignmentId { get; set; }
    public long ConsultantProfileId { get; set; }
}
