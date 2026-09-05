using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.Accounting.Domain.PatientFinance.Entities;

public sealed class PatientFinancialCase : BaseAuditableEntity<Guid>
{
    public Guid PatientId { get; set; }
    public DentalServiceType Service { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PrePaymentAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public PatientFinancialAgreementType AgreementType { get; set; }
    public PatientFinancialCaseStatus Status { get; set; } = PatientFinancialCaseStatus.Active;
    public Guid CreatedByUserId { get; set; }
    public User Patient { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<PatientCheque> Cheques { get; set; } = [];
    public ICollection<PatientPromissoryNote> PromissoryNotes { get; set; } = [];
    public ICollection<PatientDebt> Debts { get; set; } = [];
    public ICollection<PatientFinancialTransaction> Transactions { get; set; } = [];
}
