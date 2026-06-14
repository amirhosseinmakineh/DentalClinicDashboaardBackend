using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.IDomainService
{
    public interface ILeadReportDomainService
    {
        LeadAssignmentState MapCallResultToState(LeadCallResult callResult);
    }
}
