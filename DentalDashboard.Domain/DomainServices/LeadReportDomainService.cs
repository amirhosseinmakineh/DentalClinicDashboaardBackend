using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;

namespace DentalDashboard.Domain.DomainServices
{
    public class LeadReportDomainService : ILeadReportDomainService
    {
        public LeadAssignmentState MapCallResultToState(LeadCallResult callResult)
        {
            return callResult switch
            {
                LeadCallResult.Converted => LeadAssignmentState.Converted,
                LeadCallResult.Rejected => LeadAssignmentState.Rejected,
                LeadCallResult.WrongNumber => LeadAssignmentState.Rejected,
                LeadCallResult.NoAnswer => LeadAssignmentState.Contacted,
                LeadCallResult.NeedFollowUp => LeadAssignmentState.Contacted,
                LeadCallResult.Contacted => LeadAssignmentState.Contacted,
                _ => LeadAssignmentState.Contacted
            };
        }
    }
}
