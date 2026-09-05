using DentalDashboard.Accounting.Contracts.Commands;
using FluentValidation;

namespace DentalDashboard.Accounting.Application.Validators;

public sealed class UpdateExpenseCategoryCommandValidator
    : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        RuleFor(item => item.Id).GreaterThan(0);
        RuleFor(item => item.Title)
            .NotEmpty().WithMessage("عنوان دسته‌بندی هزینه الزامی است")
            .MaximumLength(100).WithMessage("عنوان دسته‌بندی هزینه حداکثر ۱۰۰ کاراکتر است");
    }
}
