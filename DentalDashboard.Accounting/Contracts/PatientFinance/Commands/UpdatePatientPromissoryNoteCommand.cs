using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Commands;

public sealed class UpdatePatientPromissoryNoteCommand
    : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long PromissoryNoteId { get; set; }
  public decimal Amount { get; set; }
  [JsonExtensionData]
  public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
}
