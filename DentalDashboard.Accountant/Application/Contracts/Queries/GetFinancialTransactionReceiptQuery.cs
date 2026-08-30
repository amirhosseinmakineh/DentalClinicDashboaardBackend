using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.Accountant.Application.Contracts.Queries;

public sealed class GetFinancialTransactionReceiptQuery : IQuery<FinancialTransactionReceiptResponse?>
{
    public long Id { get; set; }
}
