using System.Text.Json;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Accounting;

public sealed record AccountingActor(Guid UserId);
public sealed record SetAccountantPermissionsRequest(IReadOnlyList<string> Permissions);
public sealed record AccountingOperationRequest(string? TargetId, JsonElement Data);
public sealed record AccountingOperationResponse(string Message, object? Data = null);
public sealed record AccountingPageQuery(AccountingActor Actor, AccountingSection Section, AccountingFilter Filter) : IQuery<AccountingPage>;
public sealed record AccountingOverviewQuery(AccountingActor Actor, AccountingFilter Filter) : IQuery<AccountingOverview>;
public sealed record AccountingCaseDetailsQuery(AccountingActor Actor, Guid CaseId) : IQuery<object?>;
public sealed record AccountingUsersQuery(AccountingActor Actor) : IQuery<IReadOnlyList<AccountantUser>>;
public sealed record AccountingOperationCommand(AccountingActor Actor, string Operation, AccountingOperationRequest Request) : ICommand<AccountingOperationResponse>;
public sealed record SetAccountantPermissionsCommand(AccountingActor Actor, Guid UserId, IReadOnlyList<string> Permissions) : ICommand<AccountingOperationResponse>;

public interface IAccountingAccess
{
    Task<AccountingAccess?> GetAsync(Guid userId, CancellationToken ct = default);
    Task<bool> AllowsAsync(Guid userId, string permission, CancellationToken ct = default);
}
public interface IAccountingOperationExecutor
{
    IReadOnlyDictionary<string, string> Permissions { get; }
    Task<AccountingOperationResponse> ExecuteAsync(string operation, string? targetId, JsonElement data, Guid actorUserId, CancellationToken ct);
}
// Bridge to the existing service-based consultant reward flow. No second calculator.
public interface IAccountingConsultantRewards
{
    Task<AccountingOperationResponse> ReviewAsync(long reservationId, Guid actor, bool approved, string? reason, CancellationToken ct);
}
public interface IAccountingAuditScope
{
    Guid? ActorUserId { get; set; }
    string Operation { get; set; }
}
public sealed class AccountingValidationException(string message) : Exception(message);
public sealed class AccountingForbiddenException : Exception;

public sealed record AccountingExportResponse(byte[] Content, string FileName, string ContentType = "text/csv; charset=utf-8");
public sealed record AccountingExportQuery(AccountingActor Actor, AccountingSection Section, AccountingFilter Filter, bool Excel = false) : IQuery<AccountingExportResponse>;
public sealed record AccountingTransactionQuery(AccountingActor Actor, long Id) : IQuery<object?>;

public sealed record AccountingFileResponse(Stream Content, string ContentType, string FileName);
public sealed record AccountingDocumentsQuery(AccountingActor Actor, Guid CaseId) : IQuery<IReadOnlyList<DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands.PatientFinancialDocumentDto>>;
public sealed record AccountingImageQuery(AccountingActor Actor, Guid CaseId, Guid DocumentId) : IQuery<AccountingFileResponse?>;
public sealed record AccountingReceiptQuery(AccountingActor Actor, long Id) : IQuery<AccountingFileResponse?>;

public sealed record AccountingCollectedAmount(decimal Amount);
