using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using System.Text.Json.Serialization;
namespace DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Commands;
public sealed class CreateSecretaryFollowUpCommand : ICommand { public long PatientId { get; set; } public bool Contacted { get; set; } public string? ContactResult { get; set; } [JsonIgnore] public Guid SecretaryUserId { get; set; } }
public sealed class UpdateSecretaryFollowUpCommand : ICommand { [JsonIgnore] public long Id { get; set; } public bool Contacted { get; set; } public string? ContactResult { get; set; } [JsonIgnore] public Guid SecretaryUserId { get; set; } }
public sealed class DeleteSecretaryFollowUpCommand : ICommand { [JsonIgnore] public long Id { get; set; } [JsonIgnore] public Guid SecretaryUserId { get; set; } }
