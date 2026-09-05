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

public sealed class AddPatientChequeCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddPatientChequeCommand, PatientFinanceIdResponse> {
  public async Task<Result<PatientFinanceIdResponse>>
  HandleAsync(AddPatientChequeCommand command, CancellationToken cancellationToken = default) {
    var validationError =
        FinanceRules.Cheque(command.Amount, command.SayadNumber, command.OwnerName, command.DueDate);
    if (validationError != null)
      return Result<PatientFinanceIdResponse>.Failure(validationError);
    if (!await repository.Cases.AnyAsync(item => item.Id == command.PatientFinancialCaseId &&
                                        item.Status ==
                                            PatientFinancialCaseStatus.Active,
                                   cancellationToken))
      return Result<PatientFinanceIdResponse>.Failure("پرونده فعال یافت نشد");
    var item = new PatientCheque {
      PatientFinancialCaseId = command.PatientFinancialCaseId, Amount = command.Amount,
      SayadNumber = command.SayadNumber.Trim(), OwnerName = command.OwnerName.Trim(),
      DueDate = command.DueDate
    };
    await repository.AddChequeAsync(item, cancellationToken);
    await unitOfWork.SaveChangesAsync();
    return Result<PatientFinanceIdResponse>.Success(new(item.Id));
  }
}
