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
                LeadCallResult.Contacted => LeadAssignmentState.Contacted,
                LeadCallResult.Rejected => LeadAssignmentState.Rejected,
                LeadCallResult.WrongNumber => LeadAssignmentState.Rejected,
                LeadCallResult.NoAnswer => LeadAssignmentState.Pending,
                LeadCallResult.NeedFollowUp => LeadAssignmentState.Pending,
                LeadCallResult.Busy => LeadAssignmentState.Pending,
                LeadCallResult.PatientHungUp => LeadAssignmentState.Pending,
                _ => LeadAssignmentState.Pending
            };
        }
    }
}
