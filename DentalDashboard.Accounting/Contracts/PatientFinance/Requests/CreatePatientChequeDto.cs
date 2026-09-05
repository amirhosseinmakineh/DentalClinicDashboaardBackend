namespace DentalDashboard.Accounting.Contracts.PatientFinance.Commands;

public sealed record CreatePatientChequeDto(
    decimal Amount,
    string SayadNumber,
    string OwnerName,
    DateTime DueDate);
