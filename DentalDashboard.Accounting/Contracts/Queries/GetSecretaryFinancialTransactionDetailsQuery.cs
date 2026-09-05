using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed class GetSecretaryFinancialTransactionDetailsQuery : IQuery<Result<SecretaryFinancialTransactionDto>?>
{
    public long Id { get; set; }
}
