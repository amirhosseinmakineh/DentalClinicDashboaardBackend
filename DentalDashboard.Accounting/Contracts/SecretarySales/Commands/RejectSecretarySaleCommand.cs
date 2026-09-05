using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Accounting.Contracts.SecretarySales;

namespace DentalDashboard.Accounting.Contracts.SecretarySales.Commands;

public sealed class RejectSecretarySaleCommand : ICommand
{
    [JsonIgnore] public long SaleId { get; set; }
    [JsonIgnore] public Guid AdminUserId { get; set; }
}
