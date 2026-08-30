using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;

public sealed class GetSecretaryFinancialTransactionReceiptQuery : IQuery<FinancialTransactionReceiptResponse?>
{
    public long Id { get; set; }
}
