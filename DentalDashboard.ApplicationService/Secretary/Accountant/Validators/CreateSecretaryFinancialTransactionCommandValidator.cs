using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Secretary.Accountant.Enums;
using FluentValidation;
using DentalDashboard.ApplicationService.Secretary.Accountant;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Validators;

public sealed partial class CreateSecretaryFinancialTransactionCommandValidator : AbstractValidator<CreateSecretaryFinancialTransactionCommand>
{
    public CreateSecretaryFinancialTransactionCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TransactionDate).NotEqual(default(DateTime));
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.CounterpartyName).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ExpenseCategoryId)
            .Null()
            .When(x => x.Type == FinancialTransactionType.Income)
            .WithMessage(SecretaryAccountConstants.IncomeCategoryMustBeEmptyMessage);
        RuleFor(x => x.ExpenseCategoryId)
            .NotNull()
            .When(x => x.Type == FinancialTransactionType.Expense)
            .WithMessage(SecretaryAccountConstants.ExpenseCategoryIsRequiredMessage);
    }
}
