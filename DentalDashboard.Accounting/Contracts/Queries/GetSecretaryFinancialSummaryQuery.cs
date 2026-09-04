using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;

public sealed class GetSecretaryFinancialSummaryQuery : IQuery<Result<SecretaryFinancialSummaryDto>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
