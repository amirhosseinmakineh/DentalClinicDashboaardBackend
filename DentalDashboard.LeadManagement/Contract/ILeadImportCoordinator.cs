namespace DentalDashboard.LeadManagement.Contract;
public interface ILeadImportCoordinator
{
    Task ProcessCycleAsync(CancellationToken cancellationToken = default);
}
