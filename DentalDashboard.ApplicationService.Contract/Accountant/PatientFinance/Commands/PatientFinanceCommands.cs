using System.Text.Json.Serialization;
using DentalDashboard.Domain.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Accountant.PatientFinance
    .Commands;

public sealed record CreatePatientChequeDto(decimal Amount, string SayadNumber,
                                            string OwnerName, DateTime DueDate);
public sealed record CreatePatientPromissoryNoteDto(string SerialNumber,
                                                    decimal Amount,
                                                    DateTime DueDate);
public sealed record PatientFinanceIdResponse(long Id);
public sealed record PatientFinancialCaseIdResponse(Guid Id);

public sealed class CreatePatientFinancialCaseCommand
    : ICommand<PatientFinancialCaseIdResponse> {
  public Guid PatientId { get; set; }
  public int ServiceId { get; set; }
  public decimal TotalAmount { get; set; }
  public PatientFinancialAgreementType AgreementType { get; set; }
  public List<CreatePatientChequeDto>? Cheques { get; set; }
  public List<CreatePatientPromissoryNoteDto>? PromissoryNotes { get; set; }
  [JsonIgnore]
  public Guid ActorUserId {
    get; set;
  }
}
public sealed class UpdatePatientFinancialCaseCommand
    : ICommand<PatientFinancialCaseIdResponse> {
  [JsonIgnore]
  public Guid Id { get; set; }
  public decimal TotalAmount { get; set; }
  public PatientFinancialAgreementType AgreementType { get; set; }
}
public sealed record CancelPatientFinancialCaseCommand(Guid Id)
    : ICommand<PatientFinancialCaseIdResponse>;
public sealed record AddPatientChequeCommand(Guid PatientFinancialCaseId,
                                             decimal Amount, string SayadNumber,
                                             string OwnerName, DateTime DueDate)
    : ICommand<PatientFinanceIdResponse>;
public sealed
    record AddPatientPromissoryNoteCommand(Guid PatientFinancialCaseId,
                                           string SerialNumber, decimal Amount,
                                           DateTime DueDate)
    : ICommand<PatientFinanceIdResponse>;
public sealed class UpdatePatientChequeStatusCommand
    : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long ChequeId { get; set; }
  public PatientChequeStatus Status { get; set; }
  [JsonIgnore]
  public Guid ActorUserId {
    get; set;
  }
}
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
public sealed class PayPatientDebtCommand : ICommand<PatientFinanceIdResponse> {
  [JsonIgnore]
  public long DebtId { get; set; }
  [JsonIgnore]
  public Guid ActorUserId {
    get; set;
  }
}
