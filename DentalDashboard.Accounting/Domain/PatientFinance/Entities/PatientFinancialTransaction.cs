using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.Accounting.Domain.PatientFinance.Entities;

public sealed class PatientFinancialTransaction : BaseAuditableEntity<long>
{
    public Guid PatientFinancialCaseId { get; set; }
    public decimal Amount { get; set; }
    public PatientFinancialTransactionType Type { get; set; } = PatientFinancialTransactionType.Payment;
    public PatientFinancialTransactionSourceType SourceType { get; set; }
    public long SourceId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public PatientFinancialCase FinancialCase { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
