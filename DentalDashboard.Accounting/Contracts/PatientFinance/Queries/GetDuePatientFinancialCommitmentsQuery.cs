using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public sealed class GetDuePatientFinancialCommitmentsQuery
    : PatientFinancePagedQuery,
      IQuery<PaginatedResult<PatientFinancialCommitmentDto>> {
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
  public PatientFinancialCommitmentType? Type { get; set; }
  public Guid? PatientId { get; set; }
}
