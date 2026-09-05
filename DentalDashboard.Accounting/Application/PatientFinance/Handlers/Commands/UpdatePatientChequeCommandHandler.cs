using DentalDashboard.Accounting.Contracts.PatientFinance
    .Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Utilities.Time;
using System.Data;

namespace DentalDashboard.Accounting.Application.PatientFinance.Handlers;

public sealed class UpdatePatientChequeCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePatientChequeCommand, PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>> HandleAsync(
      UpdatePatientChequeCommand command, CancellationToken cancellationToken = default) {
    if (command.AdditionalFields is { Count: > 0 })
      return Result<PatientFinanceIdResponse>.Failure(
          "درخواست شامل فیلد غیرمجاز است.");
    if (command.Amount <= 0)
      return Result<PatientFinanceIdResponse>.Failure(
          "مبلغ چک باید بیشتر از صفر باشد");
    if (string.IsNullOrWhiteSpace(command.OwnerName))
      return Result<PatientFinanceIdResponse>.Failure("نام صاحب چک الزامی است");
    if (command.OwnerName.Trim().Length > 200)
      return Result<PatientFinanceIdResponse>.Failure(
          "نام صاحب چک نمی‌تواند بیشتر از ۲۰۰ نویسه باشد");

    await unitOfWork.BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
    try {
      var cheque = await repository.Cheques.Include(item => item.FinancialCase)
          .FirstOrDefaultAsync(item => item.Id == command.ChequeId, cancellationToken);
      if (cheque is null) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure("چک یافت نشد");
      }
      if (cheque.Status != PatientChequeStatus.Pending) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "چک تأیید یا رد شده است و دیگر قابل ویرایش نیست.");
      }
      var otherCommitments = await repository.Cheques
          .Where(item => item.PatientFinancialCaseId == cheque.PatientFinancialCaseId &&
                      item.Id != cheque.Id)
          .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
      otherCommitments += await repository.PromissoryNotes
          .Where(item => item.PatientFinancialCaseId == cheque.PatientFinancialCaseId)
          .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
      if (otherCommitments + command.Amount > cheque.FinancialCase.TotalAmount) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "مجموع تعهدات نمی‌تواند از مبلغ کل پرونده بیشتر باشد");
      }
      cheque.Amount = command.Amount;
      cheque.OwnerName = command.OwnerName.Trim();
      cheque.UpdatedAt = DateTime.UtcNow;
      await unitOfWork.CommitAsync(cancellationToken);
      return Result<PatientFinanceIdResponse>.Success(
          new(cheque.Id), "اطلاعات چک با موفقیت ویرایش شد.");
    } catch {
      await unitOfWork.RollbackAsync(cancellationToken);
      throw;
    }
  }
}
