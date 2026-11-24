using System.Text.Json;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class ClinicAccountingOperations(ICommandDispatcher commands,
    IValidator<CreateSecretaryFinancialTransactionCommand> createValidator,
    IValidator<UpdateSecretaryFinancialTransactionCommand> updateValidator,
    IValidator<CreateExpenseCommand> categoryValidator,
    IValidator<UpdateExpenseCategoryCommand> updateCategoryValidator) : IAccountingOperationExecutor
{
    public IReadOnlyDictionary<string, string> Permissions { get; } = new Dictionary<string, string> {
        ["CreateTransaction"] = AccountingPermissions.ManageTransactions, ["UpdateTransaction"] = AccountingPermissions.ManageTransactions,
        ["DeleteTransaction"] = AccountingPermissions.Cancel, ["CreateCategory"] = AccountingPermissions.ManageCategories,
        ["UpdateCategory"] = AccountingPermissions.ManageCategories, ["DeleteCategory"] = AccountingPermissions.ManageCategories
    };
    public async Task<AccountingOperationResponse> ExecuteAsync(string operation, string? targetId, JsonElement data, Guid actorUserId, CancellationToken ct)
    {
        switch (operation) {
            case "CreateTransaction": {
                var command = AccountingPayload.Read<CreateSecretaryFinancialTransactionCommand>(data);
                command.CreatedByUserId = actorUserId;
                await Validate(createValidator, command, ct);
                var result = await commands.DispatchAsync(command, ct);
                return AccountingPayload.Result(result, result.Data);
            }
            case "UpdateTransaction": {
                var command = AccountingPayload.Read<UpdateSecretaryFinancialTransactionCommand>(data);
                command.Id = AccountingPayload.LongId(targetId);
                await Validate(updateValidator, command, ct);
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "DeleteTransaction": return AccountingPayload.Result(await commands.DispatchAsync(new DeleteSecretaryFinancialTransactionCommand(AccountingPayload.LongId(targetId)), ct));
            case "CreateCategory": {
                var command = AccountingPayload.Read<CreateExpenseCommand>(data);
                await Validate(categoryValidator, command, ct);
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "UpdateCategory": {
                var command = AccountingPayload.Read<UpdateExpenseCategoryCommand>(data);
                command.Id = AccountingPayload.LongId(targetId);
                await Validate(updateCategoryValidator, command, ct);
                return AccountingPayload.Result(await commands.DispatchAsync(command, ct));
            }
            case "DeleteCategory": return AccountingPayload.Result(await commands.DispatchAsync(new DeleteExpenseCategoryCommand(AccountingPayload.LongId(targetId)), ct));
            default: throw new AccountingValidationException("عملیات درآمد و هزینه معتبر نیست.");
        }
    }
    private static async Task Validate<T>(IValidator<T> validator, T command, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(command, ct);
        if (!result.IsValid) throw new AccountingValidationException(string.Join(" — ", result.Errors.Select(x => x.ErrorMessage)));
    }
}
