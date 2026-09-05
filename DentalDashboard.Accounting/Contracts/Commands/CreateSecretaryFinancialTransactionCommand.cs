using System.Text.Json.Serialization;
using DentalDashboard.Accounting.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.Commands;

public sealed class CreateSecretaryFinancialTransactionCommand : ICommand<CreateSecretaryFinancialTransactionResponse>
{
    public FinancialTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Subject { get; set; }
    public string? CounterpartyName { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Description { get; set; }
    public long? ExpenseCategoryId { get; set; }

    [JsonIgnore]
    public Guid CreatedByUserId { get; set; }
}
