using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Utilities.Time;
using System.Data;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

public sealed class PayPatientDebtCommandHandler(IPatientFinanceRepository repository,
                                                 IUnitOfWork unitOfWork)
    : ICommandHandler<PayPatientDebtCommand, PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(PayPatientDebtCommand command, CancellationToken cancellationToken = default) {
    await unitOfWork.BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
    try {
      var debt = await repository.Debts.Include(item => item.FinancialCase)
                  .FirstOrDefaultAsync(item => item.Id == command.DebtId, cancellationToken);
      if (debt is null || debt.Status != PatientDebtStatus.Unpaid) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "بدهی پرداخت‌نشده یافت نشد");
      }
      var hasPendingCommitment =
          await repository.Cheques.AnyAsync(
              item => item.PatientFinancialCaseId == debt.PatientFinancialCaseId &&
                   item.Status == PatientChequeStatus.Pending,
              cancellationToken) ||
          await repository.PromissoryNotes.AnyAsync(
              item => item.PatientFinancialCaseId == debt.PatientFinancialCaseId &&
                   item.Status == PatientPromissoryNoteStatus.Pending,
              cancellationToken);
      if (hasPendingCommitment) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "تا تعیین تکلیف همه چک‌ها و سفته‌های در گردش، تسویه کامل بدهی امکان‌پذیر نیست");
      }
      var st = debt.SourceType == PatientDebtSourceType.Cheque
                   ? PatientFinancialTransactionSourceType.Cheque
                   : PatientFinancialTransactionSourceType.PromissoryNote;
      if (await repository.Transactions.AnyAsync(
              item => item.SourceType == st && item.SourceId == debt.SourceId, cancellationToken)) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "این تعهد قبلاً پرداخت شده است");
      }
      var paid = await repository.Transactions
                       .Where(item => item.PatientFinancialCaseId ==
                                   debt.PatientFinancialCaseId)
                       .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
      if (paid + debt.Amount > debt.FinancialCase.TotalAmount) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "پرداخت از مبلغ کل درمان بیشتر " +
            "می‌شود");
      }
      if (debt.SourceType == PatientDebtSourceType.Cheque) {
        var source = await repository.Cheques.FirstOrDefaultAsync(
            item => item.Id == debt.SourceId &&
                 item.Status == PatientChequeStatus.Unpaid,
            cancellationToken);
        if (source is null) {
          await unitOfWork.RollbackAsync(cancellationToken);
          return Result<PatientFinanceIdResponse>.Failure(
              "چک پرداخت‌نشده مرتبط با بدهی یافت نشد");
        }
        source.Status = PatientChequeStatus.Paid;
      } else {
        var source = await repository.PromissoryNotes.FirstOrDefaultAsync(
            item => item.Id == debt.SourceId &&
                 item.Status == PatientPromissoryNoteStatus.Unpaid,
            cancellationToken);
        if (source is null) {
          await unitOfWork.RollbackAsync(cancellationToken);
          return Result<PatientFinanceIdResponse>.Failure(
              "سفته پرداخت‌نشده مرتبط با بدهی یافت نشد");
        }
        source.Status = PatientPromissoryNoteStatus.Paid;
      }
      await repository.AddTransactionAsync(
          new() { PatientFinancialCaseId = debt.PatientFinancialCaseId,
                  Amount = debt.Amount, SourceType = st, SourceId = debt.SourceId,
                  CreatedByUserId = command.ActorUserId },
          cancellationToken);
      debt.Status = PatientDebtStatus.Paid;
      if (paid + debt.Amount == debt.FinancialCase.TotalAmount)
        debt.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
      await unitOfWork.CommitAsync(cancellationToken);
      return Result<PatientFinanceIdResponse>.Success(new(debt.Id));
    } catch {
      await unitOfWork.RollbackAsync(cancellationToken);
      throw;
    }
  }
}
