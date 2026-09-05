using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

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
