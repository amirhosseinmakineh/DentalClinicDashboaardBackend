using DentalDashboard.Accountant.Application.Contracts.Commands;
using FluentValidation;

namespace DentalDashboard.Accountant.Application.Validators;

public sealed partial class CreateFinancialTransactionCommandValidator
{
    public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان دسته‌بندی هزینه الزامی است")
                .MaximumLength(100).WithMessage("عنوان دسته‌بندی هزینه حداکثر ۱۰۰ کاراکتر است");
        }
    }
}
