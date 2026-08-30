using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public abstract class PatientFinancePagedQuery {
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
}
public sealed class GetPatientFinancialCasesQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientFinancialCaseDto>> {
  public string? Search { get; set; }
  public Guid? PatientId { get; set; }
  public int? ServiceId { get; set; }
  public PatientFinancialAgreementType? AgreementType { get; set; }
  public PatientFinancialCaseStatus? Status { get; set; }
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
}
public sealed
    record GetPatientFinancialCaseDetailsQuery(Guid PatientFinancialCaseId)
    : IQuery<PatientFinancialCaseDetailsDto?>;
public sealed
    record GetPatientFinancialCaseSummaryQuery(Guid PatientFinancialCaseId)
    : IQuery<PatientFinancialCaseSummaryDto?>;
public sealed record GetPatientFinancialSummaryQuery(Guid PatientId)
    : IQuery<PatientFinancialSummaryDto?>;
public sealed class GetPatientChequesQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientChequeDto>> {
  public Guid? PatientFinancialCaseId { get; set; }
  public Guid? PatientId { get; set; }
  public string? Search { get; set; }
  public PatientChequeStatus? Status { get; set; }
  public DateTime? FromDueDate { get; set; }
  public DateTime? ToDueDate { get; set; }
}
public sealed class GetPatientPromissoryNotesQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientPromissoryNoteDto>> {
  public Guid? PatientFinancialCaseId { get; set; }
  public Guid? PatientId { get; set; }
  public string? Search { get; set; }
  public PatientPromissoryNoteStatus? Status { get; set; }
  public DateTime? FromDueDate { get; set; }
  public DateTime? ToDueDate { get; set; }
}
public sealed class GetPatientDebtsQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientDebtDto>> {
  public Guid? PatientId { get; set; }
  public Guid? PatientFinancialCaseId { get; set; }
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
  public Guid? PatientId { get; set; }
  public Guid? PatientFinancialCaseId { get; set; }
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
  public Guid? PatientId { get; set; }
}

public sealed record PatientFinancialCaseDto(
    Guid Id, Guid PatientId, Guid UserId, string PatientName,
    string? PatientPhoneNumber, int ServiceId, string ServiceName, decimal TotalAmount,
    decimal TotalPaidAmount, decimal RemainingAmount, decimal TotalDebtAmount,
    PatientFinancialAgreementType AgreementType,
    PatientFinancialCaseStatus Status, DateTime CreatedAt);
public sealed record PatientFinancialCaseDetailsDto(
    PatientFinancialCaseDto Case, int ChequeCount, decimal ChequeAmount,
    int PromissoryNoteCount, decimal PromissoryNoteAmount);
public sealed record PatientChequeDto(long Id, Guid PatientFinancialCaseId,
                                      Guid PatientId, string PatientName,
                                      decimal Amount, string SayadNumber,
                                      string OwnerName, DateTime DueDate,
                                      PatientChequeStatus Status);
public sealed record PatientPromissoryNoteDto(
    long Id, Guid PatientFinancialCaseId, Guid PatientId, string PatientName,
    string SerialNumber, decimal Amount, DateTime DueDate,
    PatientPromissoryNoteStatus Status);
public sealed record PatientDebtDto(long Id, Guid PatientId, string PatientName,
                                    string? PatientPhoneNumber,
                                    Guid PatientFinancialCaseId,
                                    string ServiceName, decimal Amount,
                                    PatientDebtSourceType SourceType,
                                    long SourceId, DateTime DueDate,
                                    PatientDebtStatus Status);
public sealed record PatientFinancialTransactionDto(
    long Id, Guid PatientFinancialCaseId, Guid PatientId, decimal Amount,
    PatientFinancialTransactionType Type,
    PatientFinancialTransactionSourceType SourceType, long SourceId,
    DateTime CreatedAt);
public sealed record PatientFinancialSummaryDto(
    Guid PatientId, decimal TotalTreatmentAmount, decimal TotalPaidAmount,
    decimal RemainingAmount, decimal TotalDebtAmount,
    int ActiveFinancialCasesCount, int UnpaidChequesCount,
    int UnpaidPromissoryNotesCount);
public sealed record PatientFinancialCaseSummaryDto(
    decimal TotalAmount, decimal TotalPaidAmount, decimal RemainingAmount,
    decimal TotalChequeAmount, decimal PaidChequeAmount,
    decimal PendingChequeAmount, decimal UnpaidChequeAmount,
    decimal TotalPromissoryNoteAmount, decimal PaidPromissoryNoteAmount,
    decimal PendingPromissoryNoteAmount, decimal UnpaidPromissoryNoteAmount,
    decimal TotalDebtAmount);
public sealed record PatientFinancialCommitmentDto(
    long Id, PatientFinancialCommitmentType Type, Guid PatientFinancialCaseId,
    Guid PatientId, string PatientName, decimal Amount, DateTime DueDate,
    int Status);
