using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;

public sealed class PatientFinancialCase : BaseAuditableEntity<Guid> {
  public Guid PatientId { get; set; }
  public DentalServiceType Service { get; set; }
  public decimal TotalAmount { get; set; }
  public decimal PrePaymentAmount { get; set; }
  public decimal DepositAmount { get; set; }
  public PatientFinancialAgreementType AgreementType { get; set; }
  public PatientFinancialCaseStatus Status {
    get; set;
  } = PatientFinancialCaseStatus.Active;
  public Guid CreatedByUserId { get; set; }
  public User Patient { get; set; } = default!;
  public User CreatedByUser { get; set; } = default!;
  public ICollection<PatientCheque> Cheques { get; set; } = [];
  public ICollection<PatientPromissoryNote> PromissoryNotes { get; set; } = [];
  public ICollection<PatientDebt> Debts { get; set; } = [];
  public ICollection<PatientFinancialTransaction> Transactions {
    get; set;
  } = [];
}

public sealed class PatientCheque : BaseAuditableEntity<long> {
  public Guid PatientFinancialCaseId { get; set; }
  public decimal Amount { get; set; }
  public string SayadNumber { get; set; } = default!;
  public string OwnerName { get; set; } = default!;
  public DateTime DueDate { get; set; }
  public PatientChequeStatus Status { get; set; } = PatientChequeStatus.Pending;
  public PatientFinancialCase FinancialCase { get; set; } = default!;
}

public sealed class PatientPromissoryNote : BaseAuditableEntity<long> {
  public Guid PatientFinancialCaseId { get; set; }
  public string SerialNumber { get; set; } = default!;
  public decimal Amount { get; set; }
  public DateTime DueDate { get; set; }
  public PatientPromissoryNoteStatus Status {
    get; set;
  } = PatientPromissoryNoteStatus.Pending;
  public PatientFinancialCase FinancialCase { get; set; } = default!;
}

public sealed class PatientFinancialTransaction : BaseAuditableEntity<long> {
  public Guid PatientFinancialCaseId { get; set; }
  public decimal Amount { get; set; }
  public PatientFinancialTransactionType Type {
    get; set;
  } = PatientFinancialTransactionType.Payment;
  public PatientFinancialTransactionSourceType SourceType { get; set; }
  public long SourceId { get; set; }
  public Guid CreatedByUserId { get; set; }
  public PatientFinancialCase FinancialCase { get; set; } = default!;
  public User CreatedByUser { get; set; } = default!;
}

public sealed class PatientDebt : BaseAuditableEntity<long> {
  public Guid PatientFinancialCaseId { get; set; }
  public decimal Amount { get; set; }
  public PatientDebtSourceType SourceType { get; set; }
  public long SourceId { get; set; }
  public PatientDebtStatus Status { get; set; } = PatientDebtStatus.Unpaid;
  public DateTime DueDate { get; set; }
  public PatientFinancialCase FinancialCase { get; set; } = default!;
}
