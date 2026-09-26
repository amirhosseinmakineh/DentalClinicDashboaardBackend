using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories;

public interface ILeadAssignmentSettingRepository : IBaseRepository<long, LeadAssignmentSetting>
{
    Task<LeadAssignmentSetting?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
