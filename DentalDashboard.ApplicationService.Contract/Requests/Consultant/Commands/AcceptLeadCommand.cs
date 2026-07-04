using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class AcceptLeadCommand : ICommand<AcceptLeadResponse>
{
    public long LeadAssignmentId { get; set; }
    public long ConsultantProfileId { get; set; }
}
