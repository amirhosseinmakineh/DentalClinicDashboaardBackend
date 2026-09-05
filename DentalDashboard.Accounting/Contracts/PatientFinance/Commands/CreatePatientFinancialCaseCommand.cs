using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Commands;

public sealed class CreatePatientFinancialCaseCommand
    : ICommand<PatientFinancialCaseIdResponse> {
  public Guid PatientId { get; set; }
  public int ServiceId { get; set; }
  public decimal TotalAmount { get; set; }
  public decimal PrePaymentAmount { get; set; }
  public decimal DepositAmount { get; set; }
  public PatientFinancialAgreementType AgreementType { get; set; }
  public List<CreatePatientChequeDto>? Cheques { get; set; }
  public List<CreatePatientPromissoryNoteDto>? PromissoryNotes { get; set; }
  [JsonIgnore]
  public Guid ActorUserId {
    get; set;
  }
}
