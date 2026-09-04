using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record EligiblePatientDto(
    long PatientId,
    string FirstName,
    string LastName,
    string PhoneNumber)
{
    public long Id => PatientId;
    public long LeadAssignmentId => PatientId;
}

public sealed record CreatePatientFileResponse(
    long Id,
    long FileNumber);

public sealed record ImportPatientFileError(
    int Row,
    string Field,
    string Message);

public sealed record ImportPatientFilesResponse(
    bool Success,
    int ImportedCount,
    IReadOnlyList<ImportPatientFileError> Errors);

public sealed record PatientFilePageResponse(
    IReadOnlyList<PatientFileDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record EligiblePatientPageResponse(
    IReadOnlyList<EligiblePatientDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class GetPatientFilesQuery : IQuery<Result<PatientFilePageResponse>>
{
    public string? Search { get; init; }
    public long? FileNumber { get; init; }
    public PatientFileSourceType? SourceType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record GetPatientFileByIdQuery(long Id) : IQuery<Result<PatientFileDto>>;

public sealed class SearchPatientsEligibleForFileQuery : IQuery<Result<EligiblePatientPageResponse>>
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record CreatePatientFileCommand(
    long PatientId,
    string? Description) : ICommand<CreatePatientFileResponse>;

public sealed record UpdatePatientFileCommand(
    long Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Description) : ICommand;

public sealed record DeletePatientFileCommand(
    long Id) : ICommand;

public sealed record ImportPatientFilesCommand(
    Stream Content,
    string FileName,
    long Length) : ICommand<ImportPatientFilesResponse>;
