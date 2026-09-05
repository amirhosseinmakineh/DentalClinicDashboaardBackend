using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed class GetPatientFinancialTransactionsQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientFinancialTransactionDto>> {
  public Guid? PatientId { get; set; }
  public Guid? PatientFinancialCaseId { get; set; }
  public PatientFinancialTransactionSourceType? SourceType { get; set; }
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
}
