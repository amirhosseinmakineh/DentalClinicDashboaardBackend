using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentService
    {
        Task<LeadAssignment[]> LeadsListAsync();
        Task AddLeadsAsync();
        Task AssignPendingOfflineLeadsAsync();
        Task AssignRealTimeLeadsAsync();
        Task ExpireOverdueRealTimeLeadsAsync();
    }
}
