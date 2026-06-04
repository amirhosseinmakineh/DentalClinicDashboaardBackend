using DentalDashboard.ApplicationService.Contract.Dtos.LeadAssignment;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentService
    {
        Task<LeadAssignment[]> LeadsListAsync();
        Task AddLeadsAsync();


    }
}
