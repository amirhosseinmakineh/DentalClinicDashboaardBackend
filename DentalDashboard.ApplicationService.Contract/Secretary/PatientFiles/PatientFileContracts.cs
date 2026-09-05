using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

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

public sealed record PatientFileFinanceDto(
    Guid FinancialPatientId,
    decimal TotalTreatmentAmount,
    decimal TotalPaidAmount,
    decimal RemainingAmount,
    decimal TotalDebtAmount,
    int ActiveFinancialCasesCount,
    int UnpaidChequesCount,
    int UnpaidPromissoryNotesCount,
    IReadOnlyList<PatientFileFinancialCaseDto> Cases);

public sealed record PatientFileFinancialCaseDto(
    Guid Id,
    int ServiceId,
    string ServiceName,
    decimal TotalAmount,
    decimal TotalPaidAmount,
    decimal RemainingAmount,
    decimal TotalDebtAmount,
    PatientFinancialAgreementType AgreementType,
    PatientFinancialCaseStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<PatientFileChequeDto> Cheques,
    IReadOnlyList<PatientFilePromissoryNoteDto> PromissoryNotes,
    IReadOnlyList<PatientFileDebtDto> Debts,
    IReadOnlyList<PatientFileTransactionDto> Transactions);

public sealed record PatientFileChequeDto(
    long Id,
    decimal Amount,
    string SayadNumber,
    string OwnerName,
    DateTime DueDate,
    PatientChequeStatus Status);

public sealed record PatientFilePromissoryNoteDto(
    long Id,
    string SerialNumber,
    decimal Amount,
    DateTime DueDate,
    PatientPromissoryNoteStatus Status);

public sealed record PatientFileDebtDto(
    long Id,
    decimal Amount,
    PatientDebtSourceType SourceType,
    long SourceId,
    DateTime DueDate,
    PatientDebtStatus Status);

public sealed record PatientFileTransactionDto(
    long Id,
    decimal Amount,
    PatientFinancialTransactionType Type,
    PatientFinancialTransactionSourceType SourceType,
    long SourceId,
    DateTime CreatedAt);

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

public sealed record PatientFileFinancialIdentityResponse(
    Guid FinancialPatientId);

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

public sealed record EnsurePatientFileFinancialIdentityCommand(
    long PatientFileId) : ICommand<PatientFileFinancialIdentityResponse>;

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
