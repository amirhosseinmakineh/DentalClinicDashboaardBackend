using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Commands;

public sealed class UpdatePatientFinancialCaseCommand
    : ICommand<PatientFinancialCaseIdResponse> {
  [JsonIgnore]
  public Guid Id { get; set; }
  public decimal TotalAmount { get; set; }
  public decimal PrePaymentAmount { get; set; }
  public decimal DepositAmount { get; set; }
  public PatientFinancialAgreementType AgreementType { get; set; }
}
