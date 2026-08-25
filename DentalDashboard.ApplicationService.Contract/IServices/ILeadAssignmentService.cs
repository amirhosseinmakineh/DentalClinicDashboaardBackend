using DentalDashboard.ApplicationService.Contract.Dtos.Consultant;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentService
    {
        Task<LeadAssignment[]> LeadsListAsync(CancellationToken cancellationToken);
        Task AddLeadsAsync(CancellationToken cancellationToken = default);
        Task ReconcileMisclassifiedLeadStatesAsync();
        Task AssignRealTimeLeadsAsync(IReadOnlyCollection<long>? excludedConsultantIds = null);
        Task ExpireOverdueRealTimeLeadsAsync();
        Task<ExpireLeadRequeueResult> ExpireAndRequeueRealTimeLeadAsync(
            LeadAssignment lead,
            ConsultantProfile consultant);
        Task NotifyRealtimeLeadTakenAsync(long leadAssignmentId, long pickedByConsultantProfileId);
        Task AssignLeadToTestConsultant(IReadOnlyCollection<long>? excludedConsultantIds = null);
        Task AssignLeadToSellerConsultant(IReadOnlyCollection<long>? excludedConsultantIds = null);
        Task AssignLeadToTopSellertConsultant(IReadOnlyCollection<long>? excludedConsultantIds = null);
    }
}
