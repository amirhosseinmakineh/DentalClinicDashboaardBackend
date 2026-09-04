using System.Data;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

public sealed class UpdatePatientChequeStatusCommandHandler(
    IPatientFinanceRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePatientChequeStatusCommand, PatientFinanceIdResponse>
{
    public async Task<Result<PatientFinanceIdResponse>> HandleAsync(
        UpdatePatientChequeStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Status is not (PatientChequeStatus.Paid or PatientChequeStatus.Unpaid))
        {
            return Result<PatientFinanceIdResponse>.Failure("وضعیت مقصد معتبر نیست");
        }

        await unitOfWork.BeginTransactionAsync(
            cancellationToken,
            IsolationLevel.Serializable);

        try
        {
            var cheque = await repository.Cheques
                .Include(item => item.FinancialCase)
                .FirstOrDefaultAsync(
                    item => item.Id == command.ChequeId,
                    cancellationToken);

            if (cheque is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure("چک یافت نشد");
            }

            if (cheque.Status != PatientChequeStatus.Pending)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure(
                    "وضعیت چک قبلاً تعیین شده و قابل تغییر نیست");
            }

            if (IranTimeHelper.TodayInIran() < IranTimeHelper.GetDateInIran(cheque.DueDate))
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure(
                    "ثبت نتیجه پرداخت فقط از روز سررسید امکان‌پذیر است");
            }

            var result = command.Status == PatientChequeStatus.Paid
                ? await RegisterPaidChequeAsync(
                    cheque,
                    command.ActorUserId,
                    cancellationToken)
                : await RegisterUnpaidChequeAsync(cheque, cancellationToken);

            if (!result.IsSuccess)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure(result.Message!);
            }

            cheque.Status = command.Status;
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<PatientFinanceIdResponse>.Success(new(cheque.Id));
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result> RegisterPaidChequeAsync(
        PatientCheque cheque,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var paidAmount = await repository.Transactions
            .Where(transaction =>
                transaction.PatientFinancialCaseId == cheque.PatientFinancialCaseId)
            .SumAsync(
                transaction => (decimal?)transaction.Amount,
                cancellationToken) ?? 0;

        if (paidAmount + cheque.Amount > cheque.FinancialCase.TotalAmount)
        {
            return Result.Failure("پرداخت از مبلغ کل درمان بیشتر می‌شود");
        }

        var legacyDebts = await repository.Debts
            .Where(debt =>
                debt.SourceType == PatientDebtSourceType.Cheque &&
                debt.SourceId == cheque.Id &&
                debt.Status == PatientDebtStatus.Unpaid)
            .ToListAsync(cancellationToken);

        foreach (var legacyDebt in legacyDebts)
        {
            legacyDebt.Status = PatientDebtStatus.Cancelled;
        }

        await repository.AddTransactionAsync(
            new PatientFinancialTransaction
            {
                PatientFinancialCaseId = cheque.PatientFinancialCaseId,
                Amount = cheque.Amount,
                SourceType = PatientFinancialTransactionSourceType.Cheque,
                SourceId = cheque.Id,
                CreatedByUserId = actorUserId
            },
            cancellationToken);

        if (paidAmount + cheque.Amount == cheque.FinancialCase.TotalAmount)
        {
            cheque.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
        }

        return Result.Success();
    }

    private async Task<Result> RegisterUnpaidChequeAsync(
        PatientCheque cheque,
        CancellationToken cancellationToken)
    {
        var debtAlreadyExists = await repository.Debts.AnyAsync(
            debt =>
                debt.SourceType == PatientDebtSourceType.Cheque &&
                debt.SourceId == cheque.Id,
            cancellationToken);

        if (debtAlreadyExists)
        {
            return Result.Failure("برای این چک قبلاً بدهی ثبت شده است");
        }

        await repository.AddDebtAsync(
            new PatientDebt
            {
                PatientFinancialCaseId = cheque.PatientFinancialCaseId,
                Amount = cheque.Amount,
                SourceType = PatientDebtSourceType.Cheque,
                SourceId = cheque.Id,
                DueDate = cheque.DueDate
            },
            cancellationToken);

        return Result.Success();
    }
}
