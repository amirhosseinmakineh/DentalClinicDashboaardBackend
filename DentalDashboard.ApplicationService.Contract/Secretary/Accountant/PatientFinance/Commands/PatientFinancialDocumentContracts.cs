using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;

public sealed record PatientFinancialDocumentDto(
    Guid Id,
    Guid PatientFinancialCaseId,
    string Description,
    DateTime CreatedAt);

public sealed record PatientFinancialDocumentFileDto(
    string StoragePath,
    string ContentType);

public sealed record GetPatientFinancialDocumentsQuery(Guid CaseId)
    : IQuery<IReadOnlyList<PatientFinancialDocumentDto>>;

public sealed record GetPatientFinancialDocumentFileQuery(
    Guid CaseId,
    Guid DocumentId)
    : IQuery<PatientFinancialDocumentFileDto?>;

public sealed record AddPatientFinancialDocumentCommand(
    Guid CaseId,
    string StoragePath,
    string ContentType,
    string? Description)
    : ICommand<PatientFinancialDocumentDto>;

public sealed record DeletePatientFinancialDocumentCommand(
    Guid CaseId,
    Guid DocumentId)
    : ICommand<Guid>;
