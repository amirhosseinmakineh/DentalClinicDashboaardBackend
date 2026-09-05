using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Commands;

public sealed class PayPatientDebtCommand : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long DebtId { get; set; }
  [JsonIgnore]
  public Guid ActorUserId {
    get; set;
  }
}
