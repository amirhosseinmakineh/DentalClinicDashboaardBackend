using DentalDashboard.Domain.Models;
namespace DentalDashboard.LeadManagement.Contract;
public interface IAdminLeadSheetService { Task<AdminLeadSheet> CreateSheetAsync(string name,CancellationToken ct=default); Task<AdminSheetLead> AddLeadAsync(long sheetId,string phone,string firstName,string lastName,CancellationToken ct=default); Task<int> ProcessPendingAsync(CancellationToken ct=default); }
