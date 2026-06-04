using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface ILeadAssignmentRepository : IBaseRepository<long, LeadAssignment>
    {
        public Task<List<LeadAssignment>> GetPendingOfflineQueueAsync();
        Task<bool> HasPendingOfflineLeadsAsync(long consultantProfileId);
    }
}
