using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;

public sealed class GetSecretaryFinancialTransactionDetailsQuery : IQuery<Result<SecretaryFinancialTransactionDto>?>
{
    public long Id { get; set; }
}
