namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;

public sealed record CreatePatientPromissoryNoteDto(
    string SerialNumber,
    decimal Amount,
    DateTime DueDate);
