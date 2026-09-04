using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Commands;

public sealed class UpdatePatientPromissoryNoteCommand
    : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long PromissoryNoteId { get; set; }
  public decimal Amount { get; set; }
  [JsonExtensionData]
  public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
}
