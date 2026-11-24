using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Accounting.Services;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Accounting.Handlers;

public sealed class AccountingPageHandler(AccountingQueryService service) : IQueryHandler<AccountingPageQuery, AccountingPage>
{
    public Task<AccountingPage> HandleAsync(AccountingPageQuery query, CancellationToken ct = default) => service.PageAsync(query, ct);
}
public sealed class AccountingOverviewHandler(AccountingQueryService service) : IQueryHandler<AccountingOverviewQuery, AccountingOverview>
{
    public Task<AccountingOverview> HandleAsync(AccountingOverviewQuery query, CancellationToken ct = default) => service.OverviewAsync(query, ct);
}
public sealed class AccountingCaseDetailsHandler(AccountingQueryService service) : IQueryHandler<AccountingCaseDetailsQuery, object?>
{
    public Task<object?> HandleAsync(AccountingCaseDetailsQuery query, CancellationToken ct = default) => service.DetailsAsync(query, ct);
}
public sealed class AccountingUsersHandler(AccountingQueryService service) : IQueryHandler<AccountingUsersQuery, IReadOnlyList<AccountantUser>>
{
    public Task<IReadOnlyList<AccountantUser>> HandleAsync(AccountingUsersQuery query, CancellationToken ct = default) => service.UsersAsync(query, ct);
}
public sealed class AccountingOperationHandler(AccountingOperationService service) : ICommandHandler<AccountingOperationCommand, AccountingOperationResponse>
{
    public async Task<Result<AccountingOperationResponse>> HandleAsync(AccountingOperationCommand command, CancellationToken ct = default) =>
        Result<AccountingOperationResponse>.Success(await service.ExecuteAsync(command, ct));
}
public sealed class SetAccountantPermissionsHandler(AccountingPermissionService service) : ICommandHandler<SetAccountantPermissionsCommand, AccountingOperationResponse>
{
    public async Task<Result<AccountingOperationResponse>> HandleAsync(SetAccountantPermissionsCommand command, CancellationToken ct = default) =>
        Result<AccountingOperationResponse>.Success(await service.SetAsync(command, ct));
}

public sealed class AccountingExportHandler(AccountingExportService service) : IQueryHandler<AccountingExportQuery, AccountingExportResponse>
{
    public Task<AccountingExportResponse> HandleAsync(AccountingExportQuery query, CancellationToken ct = default) => service.ExportAsync(query, ct);
}
public sealed class AccountingTransactionHandler(AccountingQueryService service) : IQueryHandler<AccountingTransactionQuery, object?>
{
    public Task<object?> HandleAsync(AccountingTransactionQuery query, CancellationToken ct = default) => service.TransactionAsync(query, ct);
}

public sealed class AccountingDocumentsHandler(AccountingDocumentService service) : IQueryHandler<AccountingDocumentsQuery, IReadOnlyList<DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands.PatientFinancialDocumentDto>>
{
    public Task<IReadOnlyList<DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands.PatientFinancialDocumentDto>> HandleAsync(AccountingDocumentsQuery query, CancellationToken ct = default) => service.ListAsync(query, ct);
}
public sealed class AccountingImageHandler(AccountingDocumentService service) : IQueryHandler<AccountingImageQuery, AccountingFileResponse?>
{
    public Task<AccountingFileResponse?> HandleAsync(AccountingImageQuery query, CancellationToken ct = default) => service.ImageAsync(query, ct);
}
public sealed class AccountingReceiptHandler(AccountingDocumentService service) : IQueryHandler<AccountingReceiptQuery, AccountingFileResponse?>
{
    public Task<AccountingFileResponse?> HandleAsync(AccountingReceiptQuery query, CancellationToken ct = default) => service.ReceiptAsync(query, ct);
}
