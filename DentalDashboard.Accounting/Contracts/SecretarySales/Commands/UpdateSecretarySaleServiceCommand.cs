using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Accounting.Contracts.SecretarySales;

namespace DentalDashboard.Accounting.Contracts.SecretarySales.Commands;

public sealed class UpdateSecretarySaleServiceCommand : ICommand<SecretarySaleServiceDto>
{
    [JsonIgnore] public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal SecretaryReward { get; set; }
    public bool IsActive { get; set; }
}
