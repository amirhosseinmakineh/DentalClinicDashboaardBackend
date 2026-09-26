namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

public enum PatientFinancialAgreementType { PrePayment = 1, Deposit = 2 }
public enum PatientFinancialCaseStatus {
  Active = 1,
  Completed = 2,
  Cancelled = 3
}
public enum PatientChequeStatus {
  Pending = 1,
  Paid = 2,
  Unpaid = 3,
  Cancelled = 4
}
public enum PatientPromissoryNoteStatus {
  Pending = 1,
  Paid = 2,
  Unpaid = 3,
  Cancelled = 4
}
public enum PatientFinancialTransactionType { Payment = 1 }
public enum PatientFinancialTransactionSourceType {
  Cheque = 1,
  PromissoryNote = 2
}
public enum PatientDebtSourceType { Cheque = 1, PromissoryNote = 2 }
public enum PatientDebtStatus { Unpaid = 1, Paid = 2, Cancelled = 3 }
public enum PatientFinancialCommitmentType { Cheque = 1, PromissoryNote = 2 }
