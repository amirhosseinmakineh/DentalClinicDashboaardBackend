using DentalDashboard.Domain.Models;
namespace DentalDashboard.LeadManagement.Contract;
public interface IAdminLeadSheetService
{
    Task<AdminLeadSheet> CreateSheetAsync(string name, CancellationToken cancellationToken = default);

    Task<AdminSheetLead> AddLeadAsync(
        long sheetId,
        string phoneNumber,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
