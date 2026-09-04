using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Validators;

public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage("عنوان دسته‌بندی هزینه الزامی است")
            .MaximumLength(100)
            .WithMessage("عنوان دسته‌بندی هزینه حداکثر ۱۰۰ کاراکتر است");
    }
}
