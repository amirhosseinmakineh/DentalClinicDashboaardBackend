using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Accounting.Domain.Enums;
using FluentValidation;

namespace DentalDashboard.Accounting.Application.Validators;

public sealed class UpdateSecretaryFinancialTransactionCommandValidator
    : AbstractValidator<UpdateSecretaryFinancialTransactionCommand>
{
    public UpdateSecretaryFinancialTransactionCommandValidator()
    {
        RuleFor(item => item.Id).GreaterThan(0);
        RuleFor(item => item.Type).IsInEnum();
        RuleFor(item => item.Amount).GreaterThan(0);
        RuleFor(item => item.TransactionDate).NotEqual(default(DateTime));
        RuleFor(item => item.PaymentMethod).IsInEnum();
        RuleFor(item => item.Subject).MaximumLength(200);
        RuleFor(item => item.CounterpartyName).MaximumLength(200);
        RuleFor(item => item.Description).MaximumLength(1000);
        RuleFor(item => item.ExpenseCategoryId)
            .Null()
            .When(item => item.Type == FinancialTransactionType.Income)
            .WithMessage(SecretaryAccountConstants.IncomeCategoryMustBeEmptyMessage);
        RuleFor(item => item.ExpenseCategoryId)
            .NotNull()
            .When(item => item.Type == FinancialTransactionType.Expense)
            .WithMessage(SecretaryAccountConstants.ExpenseCategoryIsRequiredMessage);
    }
}
