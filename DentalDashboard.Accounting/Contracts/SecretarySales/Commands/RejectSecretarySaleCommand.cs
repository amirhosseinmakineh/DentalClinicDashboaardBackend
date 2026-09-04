using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;

public sealed class RejectSecretarySaleCommand : ICommand
{
    [JsonIgnore] public long SaleId { get; set; }
    [JsonIgnore] public Guid AdminUserId { get; set; }
}
