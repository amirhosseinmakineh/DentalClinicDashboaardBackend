namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;

public sealed record CreatePatientChequeDto(
    decimal Amount,
    string SayadNumber,
    string OwnerName,
    DateTime DueDate);
