using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed class GetSecretaryFinancialTransactionReceiptQuery : IQuery<FinancialTransactionReceiptResponse?>
{
    public long Id { get; set; }
}
