using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;

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
