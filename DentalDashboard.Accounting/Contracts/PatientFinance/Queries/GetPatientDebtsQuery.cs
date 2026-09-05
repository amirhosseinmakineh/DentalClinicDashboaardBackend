using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

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
