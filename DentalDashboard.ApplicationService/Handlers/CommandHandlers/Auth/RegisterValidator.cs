using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth
{
    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("نام الزامی است")
                .MaximumLength(100)
                .WithMessage("نام نباید بیشتر از ۱۰۰ کاراکتر باشد");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("نام خانوادگی الزامی است")
                .MaximumLength(100)
                .WithMessage("نام خانوادگی نباید بیشتر از ۱۰۰ کاراکتر باشد");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("شماره موبایل الزامی است")
                .Matches(@"^09\d{9}$")
                .WithMessage("شماره موبایل معتبر نیست");

            RuleFor(x => x.PasswordHash)
                .NotEmpty()
                .WithMessage("رمز عبور الزامی است")
                .MinimumLength(8)
                .WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد")
                .MaximumLength(100)
                .WithMessage("رمز عبور نباید بیشتر از ۱۰۰ کاراکتر باشد");

            RuleFor(x => x.BirthDate)
                .LessThan(DateTime.Now)
                .WithMessage("تاریخ تولد معتبر نیست");

            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithMessage("جنسیت معتبر نیست");
        }
    }
}
