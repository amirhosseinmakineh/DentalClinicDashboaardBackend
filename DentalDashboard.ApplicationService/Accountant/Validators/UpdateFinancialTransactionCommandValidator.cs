using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.Domain.Accountant.Enums;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Accountant.Validators;

public sealed class UpdateFinancialTransactionCommandValidator
    : AbstractValidator<UpdateFinancialTransactionCommand>
{
    public UpdateFinancialTransactionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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
            .WithMessage(AccountantConstants.IncomeCategoryMustBeEmptyMessage);
        RuleFor(x => x.ExpenseCategoryId)
            .NotNull()
            .When(x => x.Type == FinancialTransactionType.Expense)
            .WithMessage(AccountantConstants.ExpenseCategoryIsRequiredMessage);
    }
}
