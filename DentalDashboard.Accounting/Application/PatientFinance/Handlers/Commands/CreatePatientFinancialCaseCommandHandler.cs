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

public sealed class CreatePatientFinancialCaseCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePatientFinancialCaseCommand,
                      PatientFinancialCaseIdResponse> {
  public async Task<Result<PatientFinancialCaseIdResponse>>
  HandleAsync(CreatePatientFinancialCaseCommand command,
              CancellationToken cancellationToken = default) {
    if (command.ActorUserId == Guid.Empty)
      return Result<PatientFinancialCaseIdResponse>.Failure("کاربر معتبر نیست");
    if (command.TotalAmount <= 0)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "مبلغ کل باید بیشتر از صفر باشد");
    if (command.PrePaymentAmount < 0 || command.DepositAmount < 0 ||
        command.PrePaymentAmount > command.TotalAmount || command.DepositAmount > command.TotalAmount)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "مبلغ پیش‌پرداخت یا ودیعه معتبر نیست");
    if (!Enum.IsDefined(command.AgreementType))
      return Result<PatientFinancialCaseIdResponse>.Failure("نوع توافق معتبر نیست");
    if (!Enum.IsDefined(typeof(DentalServiceType), command.ServiceId))
      return Result<PatientFinancialCaseIdResponse>.Failure("خدمت معتبر نیست");
    if (command.PatientId == Guid.Empty)
      return Result<PatientFinancialCaseIdResponse>.Failure("بیمار معتبر نیست");
    var patient = await repository.Patients.AsNoTracking()
        .Where(item => !item.IsDeleted && item.Id == command.PatientId &&
                    item.PatientProfile != null && !item.PatientProfile.IsDeleted)
        .Select(item => new { item.Id })
        .SingleOrDefaultAsync(cancellationToken);
    if (patient is null)
      return Result<PatientFinancialCaseIdResponse>.Failure("بیمار معتبر نیست");
    var cheques = command.Cheques ?? [];
    var notes = command.PromissoryNotes ?? [];
    if (command.AgreementType == PatientFinancialAgreementType.PrePayment &&
        cheques.Count == 0 && notes.Count == 0)
      return Result<PatientFinancialCaseIdResponse>.Failure(
          "برای پیش‌پرداخت ثبت حداقل یک چک یا سفته الزامی " +
          "است");
    foreach (var item in cheques) {
      var validationError =
          FinanceRules.Cheque(item.Amount, item.SayadNumber, item.OwnerName, item.DueDate);
      if (validationError != null)
        return Result<PatientFinancialCaseIdResponse>.Failure(validationError);
    }
    foreach (var item in notes) {
      var validationError = FinanceRules.Note(item.Amount, item.SerialNumber, item.DueDate);
      if (validationError != null)
        return Result<PatientFinancialCaseIdResponse>.Failure(validationError);
    }
    var entity = new PatientFinancialCase {
      PatientId = patient.Id, Service = (DentalServiceType)command.ServiceId,
      TotalAmount = command.TotalAmount, PrePaymentAmount = command.PrePaymentAmount,
      DepositAmount = command.DepositAmount, AgreementType = command.AgreementType,
      CreatedByUserId = command.ActorUserId
    };
    foreach (var item in cheques)
      entity.Cheques.Add(new PatientCheque { Amount = item.Amount,
                                             SayadNumber = item.SayadNumber.Trim(),
                                             OwnerName = item.OwnerName.Trim(),
                                             DueDate = item.DueDate });
    foreach (var item in notes)
      entity.PromissoryNotes.Add(
          new PatientPromissoryNote { Amount = item.Amount,
                                      SerialNumber = item.SerialNumber.Trim(),
                                      DueDate = item.DueDate });
    await repository.AddCaseAsync(entity, cancellationToken);
    await unitOfWork.SaveChangesAsync();
    return Result<PatientFinancialCaseIdResponse>.Success(new(entity.Id));
  }
}
