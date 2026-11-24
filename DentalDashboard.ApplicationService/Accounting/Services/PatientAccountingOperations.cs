using System.Text.Json;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Queries;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class PatientAccountingOperations(ICommandDispatcher commands, IQueryDispatcher queries,
    AccountingSettlementService settlements, DentalDashboard.Domain.IRepositories.IUnitOfWork unitOfWork) : IAccountingOperationExecutor
{
    public IReadOnlyDictionary<string, string> Permissions { get; } = new Dictionary<string, string> {
        ["UpdatePatient"] = AccountingPermissions.EditPatients, ["CancelPatient"] = AccountingPermissions.Cancel,
        ["UpdateCheque"] = AccountingPermissions.EditDocuments, ["UpdateNote"] = AccountingPermissions.EditDocuments,
        ["DeleteCheque"] = AccountingPermissions.Cancel, ["DeleteNote"] = AccountingPermissions.Cancel,
        ["ChequeStatus"] = AccountingPermissions.Collect, ["NoteStatus"] = AccountingPermissions.Collect,
        ["PayDebt"] = AccountingPermissions.Collect, ["PayInstallment"] = AccountingPermissions.Collect,
        ["AddGuaranteeCheque"] = AccountingPermissions.EditDocuments, ["AddGuaranteeNote"] = AccountingPermissions.EditDocuments,
        ["CollectGuarantee"] = AccountingPermissions.Collect
    };
    public async Task<AccountingOperationResponse> ExecuteAsync(string operation, string? targetId, JsonElement data, Guid actorUserId, CancellationToken ct)
    {
        switch (operation) {
            case "UpdatePatient": {
                await unitOfWork.BeginTransactionAsync(ct, System.Data.IsolationLevel.Serializable);
                try {
                    var command = AccountingPayload.Read<UpdatePatientFinancialCaseCommand>(data);
                    if (!Enum.IsDefined(command.AgreementType)) throw new AccountingValidationException("نوع توافق معتبر نیست.");
                    command.Id = AccountingPayload.GuidId(targetId);
                    var current = await queries.DispatchAsync(new GetPatientFinancialCaseDetailsQuery(command.Id), ct)
                        ?? throw new AccountingValidationException("پرونده مالی یافت نشد.");
                    // Units are owned by the patient chart, never entered independently in accounting.
                    command.ToothUnitCount = current.Case.ToothUnitCount;
                    try { command.ServiceUnitCounts = JsonSerializer.Deserialize<Dictionary<int, int>>(current.Case.ServiceUnitCountsJson); }
                    catch (JsonException) { throw new AccountingValidationException("اطلاعات خدمات پرونده معتبر نیست؛ چارت پرونده را بررسی کنید."); }
                    command.GuaranteeDocument = current.Case.GuaranteeDocument;
                    command.OpgImageUrl = current.Case.OpgImageUrl;
                    command.ConsultantName = current.Case.ConsultantName;
                    var result = await commands.DispatchAsync(command, ct);
                    var response = AccountingPayload.Result(result, result.Data);
                    await unitOfWork.CommitAsync(ct);
                    return response;
                } catch { await unitOfWork.RollbackAsync(CancellationToken.None); throw; }
            }
            case "AddGuaranteeCheque": {
                var input = AccountingPayload.Read<CreatePatientChequeDto>(data);
                return AccountingPayload.Result(await commands.DispatchAsync(new AddPatientChequeCommand(
                    AccountingPayload.GuidId(targetId), input.Amount, input.SayadNumber, input.OwnerName, input.DueDate, true), ct));
            }
            case "AddGuaranteeNote": {
                var input = AccountingPayload.Read<CreatePatientPromissoryNoteDto>(data);
                return AccountingPayload.Result(await commands.DispatchAsync(new AddPatientPromissoryNoteCommand(
                    AccountingPayload.GuidId(targetId), input.SerialNumber, input.Amount, input.DueDate, true), ct));
            }
            case "CancelPatient": return AccountingPayload.Result(await commands.DispatchAsync(new CancelPatientFinancialCaseCommand(AccountingPayload.GuidId(targetId)), ct));
            case "UpdateCheque": {
                var command = AccountingPayload.Read<UpdatePatientChequeCommand>(data);
                command.ChequeId = AccountingPayload.LongId(targetId);
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "UpdateNote": {
                var command = AccountingPayload.Read<UpdatePatientPromissoryNoteCommand>(data);
                command.PromissoryNoteId = AccountingPayload.LongId(targetId);
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "DeleteCheque": return AccountingPayload.Result(await commands.DispatchAsync(new DeletePatientChequeCommand(AccountingPayload.LongId(targetId)), ct));
            case "DeleteNote": return AccountingPayload.Result(await commands.DispatchAsync(new DeletePatientPromissoryNoteCommand(AccountingPayload.LongId(targetId)), ct));
            case "ChequeStatus": {
                var command = AccountingPayload.Read<UpdatePatientChequeStatusCommand>(data);
                command.ChequeId = AccountingPayload.LongId(targetId); command.ActorUserId = actorUserId;
                if ((int)command.Status == 4) throw new AccountingValidationException("برای لغو سند از عملیات حذف/لغو و مجوز مربوط استفاده کنید.");
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "NoteStatus": {
                var command = AccountingPayload.Read<UpdatePatientPromissoryNoteStatusCommand>(data);
                command.PromissoryNoteId = AccountingPayload.LongId(targetId); command.ActorUserId = actorUserId;
                if ((int)command.Status == 4) throw new AccountingValidationException("برای لغو سند از عملیات حذف/لغو استفاده کنید.");
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "PayDebt": return AccountingPayload.Result(await commands.DispatchAsync(new PayPatientDebtCommand { DebtId = AccountingPayload.LongId(targetId), ActorUserId = actorUserId }, ct));
            case "PayInstallment": return await settlements.PayInstallmentAsync(AccountingPayload.LongId(targetId), actorUserId, ct);
            case "CollectGuarantee": return await settlements.CollectGuaranteeAsync(AccountingPayload.LongId(targetId), actorUserId, ct);
            default: throw new AccountingValidationException("عملیات مالی بیمار معتبر نیست.");
        }
    }
}
