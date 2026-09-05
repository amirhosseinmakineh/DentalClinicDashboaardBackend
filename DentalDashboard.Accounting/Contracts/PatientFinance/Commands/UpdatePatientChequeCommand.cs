using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
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
