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

public sealed class CancelPatientFinancialCaseCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelPatientFinancialCaseCommand,
                      PatientFinancialCaseIdResponse> {
  public async Task<Result<PatientFinancialCaseIdResponse>>
  HandleAsync(CancelPatientFinancialCaseCommand command,
              CancellationToken cancellationToken = default) {
    await unitOfWork.BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
    try {
      var item = await repository.Cases.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
      if (item is null) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinancialCaseIdResponse>.Failure("پرونده یافت نشد");
      }
      if (item.Status != PatientFinancialCaseStatus.Active) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinancialCaseIdResponse>.Failure(
            "فقط پرونده فعال قابل لغو است");
      }
      if (item.AgreementType != PatientFinancialAgreementType.Deposit) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinancialCaseIdResponse>.Failure(
            "لغو مالی فقط برای توافق ودیعه امکان‌پذیر است.");
      }
      if (await repository.Transactions.AnyAsync(transaction =>
              transaction.PatientFinancialCaseId == command.Id &&
              transaction.Type == PatientFinancialTransactionType.Payment, cancellationToken)) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinancialCaseIdResponse>.Failure(
            "پس از ثبت اولین پرداخت، لغو مالی بیمار امکان‌پذیر نیست.");
      }
      item.Status = PatientFinancialCaseStatus.Cancelled;
      item.UpdatedAt = DateTime.UtcNow;
      await unitOfWork.CommitAsync(cancellationToken);
      return Result<PatientFinancialCaseIdResponse>.Success(new(item.Id));
    } catch {
      await unitOfWork.RollbackAsync(cancellationToken);
      throw;
    }
  }
}
