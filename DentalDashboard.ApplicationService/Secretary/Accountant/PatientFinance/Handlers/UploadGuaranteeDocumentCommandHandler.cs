using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

public sealed class UploadGuaranteeDocumentCommandHandler(
    IPatientFinanceRepository repo,
    IUnitOfWork uow,
    IFileStorageService storage)
    : ICommandHandler<UploadGuaranteeDocumentCommand, GuaranteeDocumentResponse>
{
    public async Task<Result<GuaranteeDocumentResponse>> HandleAsync(
        UploadGuaranteeDocumentCommand c,
        CancellationToken ct = default)
    {
        var x = await repo.Cases.FirstOrDefaultAsync(x => x.Id == c.CaseId, ct);

        if (x is null)
            return Result<GuaranteeDocumentResponse>.Failure("پرونده یافت نشد");

        storage.Delete(x.GuaranteeDocument);

        x.GuaranteeDocument = string.IsNullOrEmpty(c.FilePath) ? null : c.FilePath;
        x.UpdatedAt = DateTime.UtcNow;

        await uow.SaveChangesAsync();

        return Result<GuaranteeDocumentResponse>.Success(new(x.Id, x.GuaranteeDocument));
    }
}
