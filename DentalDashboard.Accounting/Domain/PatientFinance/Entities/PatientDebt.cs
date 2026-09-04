using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;

public sealed class PatientDebt : BaseAuditableEntity<long>
{
    public Guid PatientFinancialCaseId { get; set; }
    public decimal Amount { get; set; }
    public PatientDebtSourceType SourceType { get; set; }
    public long SourceId { get; set; }
    public PatientDebtStatus Status { get; set; } = PatientDebtStatus.Unpaid;
    public DateTime DueDate { get; set; }
    public PatientFinancialCase FinancialCase { get; set; } = null!;
}
