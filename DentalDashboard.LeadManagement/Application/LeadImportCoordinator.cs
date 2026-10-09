using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.LeadManagement.Contract;
using DentalDashboard.LeadManagement.Contract;
namespace DentalDashboard.LeadManagement.Application;
/// <summary>Orchestrates source imports only. Existing lead creation and allocation rules remain untouched.</summary>
public sealed class LeadImportCoordinator(ILeadAssignmentService yektanet, IAdminLeadSheetService adminSheets) : ILeadImportCoordinator
{
    public async Task ProcessCycleAsync(CancellationToken cancellationToken = default)
    {
        // The order is intentional: Yektanet must complete before admin-sheet rows are processed.
        await yektanet.AddLeadsAsync();
        cancellationToken.ThrowIfCancellationRequested();
        await adminSheets.ProcessPendingAsync(cancellationToken);
    }
}
