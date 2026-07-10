using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface ILeadAssignmentRepository : IBaseRepository<long, LeadAssignment>
    {
        Task<bool> HasActiveRealTimeLeadAsync(long consultantProfileId);
        Task<List<LeadAssignment>> GetUnassignedRealTimeLeadsAsync(int take);
        Task<List<LeadAssignment>> GetRealtimeLeadsForDispatchAsync(int take, TimeSpan redispatchInterval);
        Task<List<LeadAssignment>> GetExpiredRealTimeLeadsAsync(DateTime now);
        Task<int> CountUnassignedRealTimeLeadsAsync();
        Task<HashSet<string>> GetExistingPhoneNumbersAsync(IEnumerable<string> phoneNumbers);
        Task<LeadAssignment?> GetByIdAndConsultantAsync(long leadAssignmentId, long consultantProfileId);
        Task<List<LeadAssignment>> GetAssignedLeadsPendingNotificationAsync();
        Task<int> GetTodayPickupCountAsync(long consultantProfileId);
        Task<bool> TryPickupLeadAsync(long leadAssignmentId, long consultantProfileId, CancellationToken cancellationToken);
    }
}
