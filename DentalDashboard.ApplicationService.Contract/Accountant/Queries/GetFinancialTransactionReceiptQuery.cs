using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Queries;

public sealed class GetFinancialTransactionReceiptQuery : IQuery<FinancialTransactionReceiptResponse?>
{
    public long Id { get; set; }
}
