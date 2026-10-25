using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

public sealed class GetPatientFinancialDocumentsQueryHandler(
    IPatientFinanceRepository repository)
    : IQueryHandler<GetPatientFinancialDocumentsQuery, IReadOnlyList<PatientFinancialDocumentDto>>
{
    public async Task<IReadOnlyList<PatientFinancialDocumentDto>> HandleAsync(
        GetPatientFinancialDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        return await repository.Documents
            .AsNoTracking()
            .Where(document => document.PatientFinancialCaseId == query.CaseId)
            .OrderByDescending(document => document.CreatedAt)
            .Select(document => new PatientFinancialDocumentDto(
                document.Id,
                document.PatientFinancialCaseId,
                document.Description ?? string.Empty,
                document.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetPatientFinancialDocumentFileQueryHandler(
    IPatientFinanceRepository repository)
    : IQueryHandler<GetPatientFinancialDocumentFileQuery, PatientFinancialDocumentFileDto?>
{
    public Task<PatientFinancialDocumentFileDto?> HandleAsync(
        GetPatientFinancialDocumentFileQuery query,
        CancellationToken cancellationToken = default)
    {
        return repository.Documents
            .AsNoTracking()
            .Where(document =>
                document.Id == query.DocumentId &&
                document.PatientFinancialCaseId == query.CaseId)
            .Select(document => new PatientFinancialDocumentFileDto(
                document.StoragePath,
                document.ContentType))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed class AddPatientFinancialDocumentCommandHandler(
    IPatientFinanceRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddPatientFinancialDocumentCommand, PatientFinancialDocumentDto>
{
    public async Task<Result<PatientFinancialDocumentDto>> HandleAsync(
        AddPatientFinancialDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.Cases.AnyAsync(
                financialCase => financialCase.Id == command.CaseId,
                cancellationToken))
        {
            return Result<PatientFinancialDocumentDto>.Failure("پرونده مالی یافت نشد");
        }

        var documentCount = await repository.Documents.CountAsync(
            document => document.PatientFinancialCaseId == command.CaseId,
            cancellationToken);
        if (documentCount >= 20)
        {
            return Result<PatientFinancialDocumentDto>.Failure("حداکثر ۲۰ تصویر برای هر پرونده مجاز است");
        }

        var document = new PatientFinancialDocument
        {
            Id = Guid.NewGuid(),
            PatientFinancialCaseId = command.CaseId,
            StoragePath = command.StoragePath,
            ContentType = command.ContentType,
            Description = command.Description?.Trim()
        };

        await repository.AddDocumentAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        return Result<PatientFinancialDocumentDto>.Success(new(
            document.Id,
            document.PatientFinancialCaseId,
            document.Description ?? string.Empty,
            document.CreatedAt));
    }
}

public sealed class DeletePatientFinancialDocumentCommandHandler(
    IPatientFinanceRepository repository,
    IUnitOfWork unitOfWork,
    IFileStorageService storage)
    : ICommandHandler<DeletePatientFinancialDocumentCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        DeletePatientFinancialDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await repository.Documents.SingleOrDefaultAsync(
            entry =>
                entry.Id == command.DocumentId &&
                entry.PatientFinancialCaseId == command.CaseId,
            cancellationToken);

        if (document is null)
        {
            return Result<Guid>.Failure("مدرک یافت نشد");
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();
        storage.DeletePatientDocument(document.StoragePath);

        return Result<Guid>.Success(document.Id);
    }
}
