using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.Accounting.Domain.PatientFinance.Entities;

public sealed class PatientCheque : BaseAuditableEntity<long>
{
    public Guid PatientFinancialCaseId { get; set; }
    public decimal Amount { get; set; }
    public string SayadNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public PatientChequeStatus Status { get; set; } = PatientChequeStatus.Pending;
    public PatientFinancialCase FinancialCase { get; set; } = null!;
}
