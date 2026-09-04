using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;

public sealed class CreateSecretarySaleCommand : ICommand<SecretarySaleCreatedDto>
{
    [JsonIgnore] public Guid SecretaryUserId { get; set; }
    public Guid PatientUserId { get; set; }
    public long ServiceId { get; set; }
}
