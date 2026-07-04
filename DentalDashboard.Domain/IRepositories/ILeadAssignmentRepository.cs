using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface ILeadAssignmentRepository : IBaseRepository<long, LeadAssignment>
    {
        Task<List<LeadAssignment>> GetPendingOfflineQueueAsync();
        Task<bool> HasPendingOfflineLeadsAsync(long consultantProfileId);
        Task<int> CountPendingOfflineLeadsAsync(long consultantProfileId);
        Task<bool> HasActiveRealTimeLeadAsync(long consultantProfileId);
        Task<List<LeadAssignment>> GetPendingOfflineLeadsAsync(int take);
        Task<List<LeadAssignment>> GetUnassignedRealTimeLeadsAsync(int take);
        Task<List<LeadAssignment>> GetExpiredRealTimeLeadsAsync(DateTime now);
        Task<int> CountUnassignedRealTimeLeadsAsync();
        Task<HashSet<long>> GetConsultantIdsWithPendingOfflineLeadsAsync(IEnumerable<long> consultantProfileIds);
        Task<HashSet<string>> GetExistingPhoneNumbersAsync(IEnumerable<string> phoneNumbers);
        Task<LeadAssignment?> GetByIdAndConsultantAsync(long leadAssignmentId, long consultantProfileId);
        Task<Dictionary<long, int>> GetDailyAssignedOfflineLeadCountsAsync(IEnumerable<long> consultantProfileIds, DateTime day);
        Task<List<LeadAssignment>> GetAssignedLeadsPendingNotificationAsync();
        Task<List<LeadAssignment>> GetBroadcastingLeadsAsync(long consultantProfileId, int take = 20);
        Task<List<long>> GetDismissedBroadcastLeadIdsAsync(long consultantProfileId);
        Task<bool> IsBroadcastDismissedAsync(long leadAssignmentId, long consultantProfileId);
        Task AddBroadcastDismissalAsync(long leadAssignmentId, long consultantProfileId);
        Task<List<LeadAssignment>> GetStaleBroadcastingLeadsAsync(DateTime now);
        Task<List<LeadAssignment>> GetPendingBroadcastRealTimeLeadsAsync(int take);
    }
}
