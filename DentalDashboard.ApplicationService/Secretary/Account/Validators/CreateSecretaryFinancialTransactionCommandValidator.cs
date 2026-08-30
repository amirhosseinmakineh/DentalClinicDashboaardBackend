using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;
using DentalDashboard.Domain.Secretary.Account.Enums;
using FluentValidation;
using DentalDashboard.ApplicationService.Secretary.Account;

namespace DentalDashboard.ApplicationService.Secretary.Account.Validators;

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
        RuleFor(x => x.TrackingNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ReceiptUrl).MaximumLength(500);
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
