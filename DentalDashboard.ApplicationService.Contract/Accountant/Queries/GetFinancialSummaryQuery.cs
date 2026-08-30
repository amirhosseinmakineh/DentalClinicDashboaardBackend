using DentalDashboard.ApplicationService.Contract.Accountant.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Queries;

public sealed class GetFinancialSummaryQuery : IQuery<Result<FinancialSummaryDto>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
