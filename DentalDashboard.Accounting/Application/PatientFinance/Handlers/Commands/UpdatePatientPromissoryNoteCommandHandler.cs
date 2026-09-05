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

public sealed class UpdatePatientPromissoryNoteCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePatientPromissoryNoteCommand,
                      PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>> HandleAsync(
      UpdatePatientPromissoryNoteCommand command, CancellationToken cancellationToken = default) {
    if (command.AdditionalFields is { Count: > 0 })
      return Result<PatientFinanceIdResponse>.Failure(
          "درخواست شامل فیلد غیرمجاز است.");
    if (command.Amount <= 0)
      return Result<PatientFinanceIdResponse>.Failure(
          "مبلغ سفته باید بیشتر از صفر باشد");

    await unitOfWork.BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
    try {
      var note = await repository.PromissoryNotes.Include(item => item.FinancialCase)
          .FirstOrDefaultAsync(item => item.Id == command.PromissoryNoteId, cancellationToken);
      if (note is null) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure("سفته یافت نشد");
      }
      if (note.Status != PatientPromissoryNoteStatus.Pending) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "سفته تأیید یا رد شده است و دیگر قابل ویرایش نیست.");
      }
      var otherCommitments = await repository.PromissoryNotes
          .Where(item => item.PatientFinancialCaseId == note.PatientFinancialCaseId &&
                      item.Id != note.Id)
          .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
      otherCommitments += await repository.Cheques
          .Where(item => item.PatientFinancialCaseId == note.PatientFinancialCaseId)
          .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0;
      if (otherCommitments + command.Amount > note.FinancialCase.TotalAmount) {
        await unitOfWork.RollbackAsync(cancellationToken);
        return Result<PatientFinanceIdResponse>.Failure(
            "مجموع تعهدات نمی‌تواند از مبلغ کل پرونده بیشتر باشد");
      }
      note.Amount = command.Amount;
      note.UpdatedAt = DateTime.UtcNow;
      await unitOfWork.CommitAsync(cancellationToken);
      return Result<PatientFinanceIdResponse>.Success(
          new(note.Id), "اطلاعات سفته با موفقیت ویرایش شد.");
    } catch {
      await unitOfWork.RollbackAsync(cancellationToken);
      throw;
    }
  }
}
