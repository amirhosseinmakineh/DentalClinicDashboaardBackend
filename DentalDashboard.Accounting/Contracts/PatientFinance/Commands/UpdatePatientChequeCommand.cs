using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Commands;

public sealed class UpdatePatientChequeCommand
    : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long ChequeId { get; set; }
  public decimal Amount { get; set; }
  public string OwnerName { get; set; } = string.Empty;
  [JsonExtensionData]
  public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
}
