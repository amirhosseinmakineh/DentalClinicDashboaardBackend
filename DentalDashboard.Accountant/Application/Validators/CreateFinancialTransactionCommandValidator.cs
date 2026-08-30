using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Accountant.Application.Contracts.Queries;
using DentalDashboard.Accountant.Domain.Enums;
using FluentValidation;
using DentalDashboard.Accountant.Application;

namespace DentalDashboard.Accountant.Application.Validators;

public sealed partial class CreateFinancialTransactionCommandValidator : AbstractValidator<CreateFinancialTransactionCommand>
{
    public CreateFinancialTransactionCommandValidator()
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
            .WithMessage(AccountantConstants.IncomeCategoryMustBeEmptyMessage);
        RuleFor(x => x.ExpenseCategoryId)
            .NotNull()
            .When(x => x.Type == FinancialTransactionType.Expense)
            .WithMessage(AccountantConstants.ExpenseCategoryIsRequiredMessage);
    }
}
