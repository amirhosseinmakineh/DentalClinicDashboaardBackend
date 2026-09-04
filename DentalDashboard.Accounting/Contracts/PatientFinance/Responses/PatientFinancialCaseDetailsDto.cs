using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public sealed record PatientFinancialCaseDetailsDto(
    PatientFinancialCaseDto Case, int ChequeCount, decimal ChequeAmount,
    int PromissoryNoteCount, decimal PromissoryNoteAmount,
    IReadOnlyList<PatientChequeDto> Cheques,
    IReadOnlyList<PatientPromissoryNoteDto> PromissoryNotes);
