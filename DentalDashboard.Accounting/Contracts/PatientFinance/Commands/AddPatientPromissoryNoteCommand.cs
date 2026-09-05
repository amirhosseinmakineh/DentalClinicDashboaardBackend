using System.Text.Json.Serialization;
using System.Text.Json;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Commands;

public sealed
    record AddPatientPromissoryNoteCommand(Guid PatientFinancialCaseId,
                                           string SerialNumber, decimal Amount,
                                           DateTime DueDate)
    : ICommand<PatientFinanceIdResponse>;
