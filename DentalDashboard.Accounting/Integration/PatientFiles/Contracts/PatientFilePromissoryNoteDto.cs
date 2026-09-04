using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFilePromissoryNoteDto(
    long Id,
    string SerialNumber,
    decimal Amount,
    DateTime DueDate,
    PatientPromissoryNoteStatus Status);
