using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Accounting.Contracts.SecretarySales;

namespace DentalDashboard.Accounting.Contracts.SecretarySales.Commands;

public sealed class SetSecretarySaleServiceStatusCommand : ICommand
{
    [JsonIgnore] public long Id { get; set; }
    public bool IsActive { get; set; }
}
