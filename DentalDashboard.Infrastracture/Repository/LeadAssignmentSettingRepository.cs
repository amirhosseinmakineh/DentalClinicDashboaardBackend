using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository;

public sealed class LeadAssignmentSettingRepository(DentalContext context)
    : BaseRepository<long, LeadAssignmentSetting>(context), ILeadAssignmentSettingRepository
{
    public Task<LeadAssignmentSetting?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        GetAll().FirstOrDefaultAsync(x => x.Id == LeadAssignmentSetting.SingletonId, cancellationToken);
}
