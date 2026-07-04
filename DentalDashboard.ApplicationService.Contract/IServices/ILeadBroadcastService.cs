namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface ILeadBroadcastService
{
    Task NotifyBroadcastAsync(long leadAssignmentId, CancellationToken cancellationToken = default);
    Task BroadcastPendingRealTimeLeadsAsync(CancellationToken cancellationToken = default);
    Task ExpireStaleBroadcastsAsync(CancellationToken cancellationToken = default);
}
