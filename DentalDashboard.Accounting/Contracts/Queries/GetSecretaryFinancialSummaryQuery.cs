using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed class GetSecretaryFinancialSummaryQuery : IQuery<Result<SecretaryFinancialSummaryDto>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
