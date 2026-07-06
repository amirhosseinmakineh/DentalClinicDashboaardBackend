using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentService
    {
        Task<LeadAssignment[]> LeadsListAsync();
        Task AddLeadsAsync();
        Task AssignOfflineLeadsToConsultantAsync(long consultantProfileId);
        Task AssignRealTimeLeadsAsync(IReadOnlyCollection<long>? excludedConsultantIds = null);
        Task ExpireOverdueRealTimeLeadsAsync();
        Task EnforceNightShiftClosureAsync();
        Task<ExpireLeadRequeueResult> ExpireAndRequeueRealTimeLeadAsync(
            LeadAssignment lead,
            ConsultantProfile consultant);
    }
}
