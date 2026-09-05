using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Commands;

public sealed class UpdatePatientPromissoryNoteStatusCommand
    : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long PromissoryNoteId { get; set; }
  public PatientPromissoryNoteStatus Status { get; set; }
  [JsonIgnore]
  public Guid ActorUserId {
    get; set;
  }
}
