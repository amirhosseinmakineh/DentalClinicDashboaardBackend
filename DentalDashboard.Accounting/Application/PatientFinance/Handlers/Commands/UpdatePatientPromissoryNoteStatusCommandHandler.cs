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

public sealed class UpdatePatientPromissoryNoteStatusCommandHandler(
    IPatientFinanceRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePatientPromissoryNoteStatusCommand, PatientFinanceIdResponse>
{
    public async Task<Result<PatientFinanceIdResponse>> HandleAsync(
        UpdatePatientPromissoryNoteStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Status is not (
                PatientPromissoryNoteStatus.Paid or PatientPromissoryNoteStatus.Unpaid))
        {
            return Result<PatientFinanceIdResponse>.Failure("وضعیت مقصد معتبر نیست");
        }

        await unitOfWork.BeginTransactionAsync(
            cancellationToken,
            IsolationLevel.Serializable);

        try
        {
            var promissoryNote = await repository.PromissoryNotes
                .Include(item => item.FinancialCase)
                .FirstOrDefaultAsync(
                    item => item.Id == command.PromissoryNoteId,
                    cancellationToken);

            if (promissoryNote is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure("سفته یافت نشد");
            }

            if (promissoryNote.Status != PatientPromissoryNoteStatus.Pending)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure(
                    "وضعیت سفته قبلاً تعیین شده و قابل تغییر نیست");
            }

            if (IranTimeHelper.TodayInIran() <
                IranTimeHelper.GetDateInIran(promissoryNote.DueDate))
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure(
                    "ثبت نتیجه پرداخت فقط از روز سررسید امکان‌پذیر است");
            }

            var result = command.Status == PatientPromissoryNoteStatus.Paid
                ? await RegisterPaidPromissoryNoteAsync(
                    promissoryNote,
                    command.ActorUserId,
                    cancellationToken)
                : await RegisterUnpaidPromissoryNoteAsync(
                    promissoryNote,
                    cancellationToken);

            if (!result.IsSuccess)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result<PatientFinanceIdResponse>.Failure(result.Message!);
            }

            promissoryNote.Status = command.Status;
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<PatientFinanceIdResponse>.Success(new(promissoryNote.Id));
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result> RegisterPaidPromissoryNoteAsync(
        PatientPromissoryNote promissoryNote,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var paidAmount = await repository.Transactions
            .Where(transaction =>
                transaction.PatientFinancialCaseId == promissoryNote.PatientFinancialCaseId)
            .SumAsync(
                transaction => (decimal?)transaction.Amount,
                cancellationToken) ?? 0;

        if (paidAmount + promissoryNote.Amount > promissoryNote.FinancialCase.TotalAmount)
        {
            return Result.Failure("پرداخت از مبلغ کل درمان بیشتر می‌شود");
        }

        var legacyDebts = await repository.Debts
            .Where(debt =>
                debt.SourceType == PatientDebtSourceType.PromissoryNote &&
                debt.SourceId == promissoryNote.Id &&
                debt.Status == PatientDebtStatus.Unpaid)
            .ToListAsync(cancellationToken);

        foreach (var legacyDebt in legacyDebts)
        {
            legacyDebt.Status = PatientDebtStatus.Cancelled;
        }

        await repository.AddTransactionAsync(
            new PatientFinancialTransaction
            {
                PatientFinancialCaseId = promissoryNote.PatientFinancialCaseId,
                Amount = promissoryNote.Amount,
                SourceType = PatientFinancialTransactionSourceType.PromissoryNote,
                SourceId = promissoryNote.Id,
                CreatedByUserId = actorUserId
            },
            cancellationToken);

        if (paidAmount + promissoryNote.Amount == promissoryNote.FinancialCase.TotalAmount)
        {
            promissoryNote.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
        }

        return Result.Success();
    }

    private async Task<Result> RegisterUnpaidPromissoryNoteAsync(
        PatientPromissoryNote promissoryNote,
        CancellationToken cancellationToken)
    {
        var debtAlreadyExists = await repository.Debts.AnyAsync(
            debt =>
                debt.SourceType == PatientDebtSourceType.PromissoryNote &&
                debt.SourceId == promissoryNote.Id,
            cancellationToken);

        if (debtAlreadyExists)
        {
            return Result.Failure("برای این سفته قبلاً بدهی ثبت شده است");
        }

        await repository.AddDebtAsync(
            new PatientDebt
            {
                PatientFinancialCaseId = promissoryNote.PatientFinancialCaseId,
                Amount = promissoryNote.Amount,
                SourceType = PatientDebtSourceType.PromissoryNote,
                SourceId = promissoryNote.Id,
                DueDate = promissoryNote.DueDate
            },
            cancellationToken);

        return Result.Success();
    }
}
