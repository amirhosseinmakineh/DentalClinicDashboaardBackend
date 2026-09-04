using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;

public sealed class SetSecretarySaleServiceStatusCommand : ICommand
{
    [JsonIgnore] public long Id { get; set; }
    public bool IsActive { get; set; }
}
