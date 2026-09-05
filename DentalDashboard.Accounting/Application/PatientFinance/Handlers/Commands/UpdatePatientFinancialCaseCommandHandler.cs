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

public sealed class UpdatePatientFinancialCaseCommandHandler(
    IPatientFinanceRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePatientFinancialCaseCommand,
                      PatientFinancialCaseIdResponse>
{
    public async Task<Result<PatientFinancialCaseIdResponse>> HandleAsync(
        UpdatePatientFinancialCaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var financialCase = await repository.Cases.FirstOrDefaultAsync(
            item => item.Id == command.Id,
            cancellationToken);

        if (financialCase is null)
        {
            return Result<PatientFinancialCaseIdResponse>.Failure("پرونده یافت نشد");
        }

        if (financialCase.Status != PatientFinancialCaseStatus.Active)
        {
            return Result<PatientFinancialCaseIdResponse>.Failure(
                "فقط پرونده فعال قابل ویرایش است");
        }

        var paidAmount = await repository.Transactions
            .Where(transaction => transaction.PatientFinancialCaseId == command.Id)
            .SumAsync(
                transaction => (decimal?)transaction.Amount,
                cancellationToken) ?? 0;

        if (command.TotalAmount <= 0 || command.TotalAmount < paidAmount)
        {
            return Result<PatientFinancialCaseIdResponse>.Failure(
                "مبلغ کل نمی‌تواند کمتر از پرداخت قطعی باشد");
        }

        if (command.PrePaymentAmount < 0 ||
            command.DepositAmount < 0 ||
            command.PrePaymentAmount > command.TotalAmount ||
            command.DepositAmount > command.TotalAmount)
        {
            return Result<PatientFinancialCaseIdResponse>.Failure(
                "مبلغ پیش‌پرداخت یا ودیعه معتبر نیست");
        }

        financialCase.TotalAmount = command.TotalAmount;
        financialCase.PrePaymentAmount = command.PrePaymentAmount;
        financialCase.DepositAmount = command.DepositAmount;
        financialCase.AgreementType = command.AgreementType;
        financialCase.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();

        return Result<PatientFinancialCaseIdResponse>.Success(new(financialCase.Id));
    }
}
