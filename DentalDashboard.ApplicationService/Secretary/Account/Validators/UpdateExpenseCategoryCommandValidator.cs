using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Secretary.Account.Validators;

public sealed class UpdateExpenseCategoryCommandValidator
    : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان دسته‌بندی هزینه الزامی است")
            .MaximumLength(100).WithMessage("عنوان دسته‌بندی هزینه حداکثر ۱۰۰ کاراکتر است");
    }
}
