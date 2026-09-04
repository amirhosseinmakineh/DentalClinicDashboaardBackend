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

public sealed class AddPatientPromissoryNoteCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddPatientPromissoryNoteCommand,
                      PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(AddPatientPromissoryNoteCommand command,
              CancellationToken cancellationToken = default) {
    var validationError = FinanceRules.Note(command.Amount, command.SerialNumber, command.DueDate);
    if (validationError != null)
      return Result<PatientFinanceIdResponse>.Failure(validationError);
    if (!await repository.Cases.AnyAsync(item => item.Id == command.PatientFinancialCaseId &&
                                        item.Status ==
                                            PatientFinancialCaseStatus.Active,
                                   cancellationToken))
      return Result<PatientFinanceIdResponse>.Failure("پرونده فعال یافت نشد");
    var item = new PatientPromissoryNote {
      PatientFinancialCaseId = command.PatientFinancialCaseId, Amount = command.Amount,
      SerialNumber = command.SerialNumber.Trim(), DueDate = command.DueDate
    };
    await repository.AddPromissoryNoteAsync(item, cancellationToken);
    await unitOfWork.SaveChangesAsync();
    return Result<PatientFinanceIdResponse>.Success(new(item.Id));
  }
}
