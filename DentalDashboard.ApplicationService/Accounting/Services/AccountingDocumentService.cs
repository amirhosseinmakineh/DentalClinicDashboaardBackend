using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class AccountingDocumentService(IAccountingAccess access, IQueryDispatcher queries, IFileStorageService storage)
{
    private async Task Require(AccountingActor actor, CancellationToken ct)
    {
        if (!await access.AllowsAsync(actor.UserId, AccountingPermissions.View, ct)) throw new AccountingForbiddenException();
    }
    public async Task<IReadOnlyList<PatientFinancialDocumentDto>> ListAsync(AccountingDocumentsQuery query, CancellationToken ct)
    {
        await Require(query.Actor, ct);
        return await queries.DispatchAsync(new GetPatientFinancialDocumentsQuery(query.CaseId), ct);
    }
    public async Task<AccountingFileResponse?> ImageAsync(AccountingImageQuery query, CancellationToken ct)
    {
        await Require(query.Actor, ct);
        var document = await queries.DispatchAsync(new GetPatientFinancialDocumentFileQuery(query.CaseId, query.DocumentId), ct);
        if (document is null) return null;
        var stream = storage.OpenPatientDocument(document.StoragePath);
        return stream is null ? null : new(stream, document.ContentType, "document");
    }
    public async Task<AccountingFileResponse?> ReceiptAsync(AccountingReceiptQuery query, CancellationToken ct)
    {
        await Require(query.Actor, ct);
        var receipt = await queries.DispatchAsync(new GetSecretaryFinancialTransactionReceiptQuery { Id = query.Id }, ct);
        return receipt is null ? null : new(new MemoryStream(receipt.Content, false), receipt.ContentType, receipt.FileName);
    }
}
