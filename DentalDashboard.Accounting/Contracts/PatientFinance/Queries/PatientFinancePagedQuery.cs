using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public abstract class PatientFinancePagedQuery {
  public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
}
