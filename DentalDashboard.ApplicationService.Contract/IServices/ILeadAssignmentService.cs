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
        Task BroadcastRealTimeLeadsAsync();
        Task ExpireStaleBroadcastsAsync();
        Task<int> RequeueUnassignedLeadsForOfflineAsync();
        Task<int> AssignPendingOfflineLeadsForConsultantAsync(long consultantProfileId);
        Task<int> SeedTestOfflineLeadsAsync(int count = 5);
    }
}
