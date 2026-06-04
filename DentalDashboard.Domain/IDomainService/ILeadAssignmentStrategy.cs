using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.IDomainService
{
    public interface ILeadAssignmentStrategy
    {
        void Assign(IList<LeadAssignment> leads, IList<ConsultantProfile> consultants);
    }

}
