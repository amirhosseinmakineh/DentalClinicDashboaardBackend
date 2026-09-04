using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
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
