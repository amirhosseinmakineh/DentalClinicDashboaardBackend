using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.IDomainService
{
    public interface ILeadDomainService
    {
        IEnumerable<LeadAssignment> GetNewLeads(IEnumerable<LeadAssignment> oldNumbers, IEnumerable<LeadAssignment> newNumbers);
        LeadAssignmentType DetermineAssignmentType(DateTime now, bool hasAvailableConsultant);
        bool IsWorkingTime(DateTime now);
        bool IsNightTime(DateTime now);
    }

}
