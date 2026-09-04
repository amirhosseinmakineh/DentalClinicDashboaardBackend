using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

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
