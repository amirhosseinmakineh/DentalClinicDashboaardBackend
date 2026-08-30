using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accountant.Application.Contracts.Queries;

public sealed class GetFinancialSummaryQuery : IQuery<Result<FinancialSummaryDto>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
