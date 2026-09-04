using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

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
