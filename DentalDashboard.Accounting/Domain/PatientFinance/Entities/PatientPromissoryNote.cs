using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.Accounting.Domain.PatientFinance.Entities;

public sealed class PatientPromissoryNote : BaseAuditableEntity<long>
{
    public Guid PatientFinancialCaseId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public PatientPromissoryNoteStatus Status { get; set; } = PatientPromissoryNoteStatus.Pending;
    public PatientFinancialCase FinancialCase { get; set; } = null!;
}
