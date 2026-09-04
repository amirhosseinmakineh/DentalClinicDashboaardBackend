using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Commands;

public sealed record AddPatientChequeCommand(Guid PatientFinancialCaseId,
                                             decimal Amount, string SayadNumber,
                                             string OwnerName, DateTime DueDate)
    : ICommand<PatientFinanceIdResponse>;
