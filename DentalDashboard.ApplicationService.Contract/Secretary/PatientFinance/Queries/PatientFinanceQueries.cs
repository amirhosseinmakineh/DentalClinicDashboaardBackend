using DentalDashboard.Domain.Secretary.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFinance
    .Queries;

public abstract class PatientFinancePagedQuery {
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
}
public sealed class GetPatientFinancialCasesQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientFinancialCaseDto>> {
  public string? Search { get; set; }
  public long? PatientId { get; set; }
  public int? ServiceId { get; set; }
  public PatientFinancialAgreementType? AgreementType { get; set; }
  public PatientFinancialCaseStatus? Status { get; set; }
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
}
public sealed
    record GetPatientFinancialCaseDetailsQuery(long PatientFinancialCaseId)
    : IQuery<PatientFinancialCaseDetailsDto?>;
public sealed
    record GetPatientFinancialCaseSummaryQuery(long PatientFinancialCaseId)
    : IQuery<PatientFinancialCaseSummaryDto?>;
public sealed record GetPatientFinancialSummaryQuery(long PatientId)
    : IQuery<PatientFinancialSummaryDto?>;
public sealed class GetPatientChequesQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientChequeDto>> {
  public long? PatientFinancialCaseId { get; set; }
  public long? PatientId { get; set; }
  public string? Search { get; set; }
  public PatientChequeStatus? Status { get; set; }
  public DateTime? FromDueDate { get; set; }
  public DateTime? ToDueDate { get; set; }
}
public sealed class GetPatientPromissoryNotesQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientPromissoryNoteDto>> {
  public long? PatientFinancialCaseId { get; set; }
  public long? PatientId { get; set; }
  public string? Search { get; set; }
  public PatientPromissoryNoteStatus? Status { get; set; }
  public DateTime? FromDueDate { get; set; }
  public DateTime? ToDueDate { get; set; }
}
public sealed class GetPatientDebtsQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientDebtDto>> {
  public long? PatientId { get; set; }
  public long? PatientFinancialCaseId { get; set; }
  public PatientDebtSourceType? SourceType { get; set; }
  public PatientDebtStatus? Status { get; set; }
  public int? Year { get; set; }
  public int? Month { get; set; }
  public DateTime? FromDueDate { get; set; }
  public DateTime? ToDueDate { get; set; }
  public string? Search { get; set; }
}
public sealed class GetPatientFinancialTransactionsQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientFinancialTransactionDto>> {
  public long? PatientId { get; set; }
  public long? PatientFinancialCaseId { get; set; }
  public PatientFinancialTransactionSourceType? SourceType { get; set; }
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
}
public sealed class GetDuePatientFinancialCommitmentsQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientFinancialCommitmentDto>> {
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
  public PatientFinancialCommitmentType? Type { get; set; }
  public long? PatientId { get; set; }
}

public sealed record PatientFinancialCaseDto(
    long Id, long PatientId, Guid UserId, string PatientName,
    string? PatientPhoneNumber, int ServiceId, string ServiceName, decimal TotalAmount,
    decimal InitialPaymentAmount, decimal TotalPaidAmount,
    decimal RemainingAmount, decimal TotalDebtAmount,
    PatientFinancialAgreementType AgreementType,
    PatientFinancialCaseStatus Status, DateTime CreatedAt);
public sealed record PatientFinancialCaseDetailsDto(
    PatientFinancialCaseDto Case, int ChequeCount, decimal ChequeAmount,
    int PromissoryNoteCount, decimal PromissoryNoteAmount);
public sealed record PatientChequeDto(long Id, long PatientFinancialCaseId,
                                      long PatientId, string PatientName,
                                      decimal Amount, string SayadNumber,
                                      string OwnerName, DateTime DueDate,
                                      PatientChequeStatus Status);
public sealed record PatientPromissoryNoteDto(
    long Id, long PatientFinancialCaseId, long PatientId, string PatientName,
    string SerialNumber, decimal Amount, DateTime DueDate,
    PatientPromissoryNoteStatus Status);
public sealed record PatientDebtDto(long Id, long PatientId, string PatientName,
                                    string? PatientPhoneNumber,
                                    long PatientFinancialCaseId,
                                    string ServiceName, decimal Amount,
                                    PatientDebtSourceType SourceType,
                                    long SourceId, DateTime DueDate,
                                    PatientDebtStatus Status);
public sealed record PatientFinancialTransactionDto(
    long Id, long PatientFinancialCaseId, long PatientId, decimal Amount,
    PatientFinancialTransactionType Type,
    PatientFinancialTransactionSourceType SourceType, long SourceId,
    DateTime CreatedAt);
public sealed record PatientFinancialSummaryDto(
    long PatientId, decimal TotalTreatmentAmount, decimal TotalPaidAmount,
    decimal RemainingAmount, decimal TotalDebtAmount,
    int ActiveFinancialCasesCount, int UnpaidChequesCount,
    int UnpaidPromissoryNotesCount);
public sealed record PatientFinancialCaseSummaryDto(
    decimal TotalAmount, decimal InitialPaymentAmount, decimal TotalPaidAmount,
    decimal RemainingAmount,
    decimal TotalChequeAmount, decimal PaidChequeAmount,
    decimal PendingChequeAmount, decimal UnpaidChequeAmount,
    decimal TotalPromissoryNoteAmount, decimal PaidPromissoryNoteAmount,
    decimal PendingPromissoryNoteAmount, decimal UnpaidPromissoryNoteAmount,
    decimal TotalDebtAmount);
public sealed record PatientFinancialCommitmentDto(
    long Id, PatientFinancialCommitmentType Type, long PatientFinancialCaseId,
    long PatientId, string PatientName, decimal Amount, DateTime DueDate,
    int Status);
