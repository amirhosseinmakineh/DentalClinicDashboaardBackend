using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.Secretary.Accountant.Entities;

public sealed class ExpenseCategory : BaseAuditableEntity<long>
{
    public string Title { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public ICollection<FinancialTransaction> FinancialTransactions { get; set; } =
        new List<FinancialTransaction>();
}
