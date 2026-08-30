using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileDto(long Id, long? PatientId, long FileNumber, string FirstName,
    string LastName, string PhoneNumber, PatientFileSourceType SourceType, DateTime CreatedAt);
public sealed record EligiblePatientDto(long PatientId, string FirstName, string LastName, string PhoneNumber);
public sealed record CreatePatientFileResponse(long Id, long FileNumber);
public sealed record ImportPatientFileError(int Row, string Field, string Message);
public sealed record ImportPatientFilesResponse(bool Success, int ImportedCount, IReadOnlyList<ImportPatientFileError> Errors);

public sealed class GetPatientFilesQuery : IQuery<Result<PaginatedResult<PatientFileDto>>>
{
    public string? Search { get; init; }
    public long? FileNumber { get; init; }
    public PatientFileSourceType? SourceType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record GetPatientFileByIdQuery(long Id) : IQuery<Result<PatientFileDto>>;

public sealed class SearchPatientsEligibleForFileQuery : IQuery<Result<PaginatedResult<EligiblePatientDto>>>
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record CreatePatientFileCommand(long PatientId) : ICommand<CreatePatientFileResponse>;
public sealed record UpdatePatientFileCommand(long Id, string FirstName, string LastName, string PhoneNumber) : ICommand;
public sealed record DeletePatientFileCommand(long Id) : ICommand;
public sealed record ImportPatientFilesCommand(Stream Content, string FileName, long Length) : ICommand<ImportPatientFilesResponse>;
