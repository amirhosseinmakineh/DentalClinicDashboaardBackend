using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileDto(
    long Id,
    long? PatientId,
    long FileNumber,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Description,
    PatientFileSourceType SourceType,
    DateTime CreatedAt,
    PatientFileFinanceDto? Finance)
{
    public Guid? FinancialPatientId { get; init; }
}
