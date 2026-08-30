using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Secretary.Account.Validators;

public sealed partial class CreateSecretaryFinancialTransactionCommandValidator
{
    public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseCommandValidator()
        {
            RuleFor(x=> x.Title).MaximumLength(200);
            RuleFor(x => x.Title).NotNull().When(x => x.Title is null).WithMessage(SecretaryAccountConstants.RequeiredExpenseTitle);
        }
    }
}
