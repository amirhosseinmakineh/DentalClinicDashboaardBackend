using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accountant.Application.Contracts.Queries;

public sealed class GetFinancialTransactionDetailsQuery : IQuery<Result<FinancialTransactionDto>?>
{
    public long Id { get; set; }
}
