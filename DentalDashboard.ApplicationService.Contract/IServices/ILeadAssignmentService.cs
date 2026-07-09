using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentService
    {
        Task<LeadAssignment[]> LeadsListAsync();
        Task AddLeadsAsync();
        Task ReconcileMisclassifiedLeadStatesAsync();
        Task AssignRealTimeLeadsAsync(IReadOnlyCollection<long>? excludedConsultantIds = null);
        Task ExpireOverdueRealTimeLeadsAsync();
        Task<ExpireLeadRequeueResult> ExpireAndRequeueRealTimeLeadAsync(
            LeadAssignment lead,
            ConsultantProfile consultant);
        Task NotifyRealtimeLeadTakenAsync(long leadAssignmentId, long pickedByConsultantProfileId);
    }
}
