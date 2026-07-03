using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth
{
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("شماره موبایل الزامی است")
                .Matches(@"^09\d{9}$")
                .WithMessage("شماره موبایل معتبر نیست");

            RuleFor(x => x.NewPasswordHash)
                .NotEmpty()
                .WithMessage("رمز عبور جدید الزامی است")
                .MinimumLength(8)
                .WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد")
                .MaximumLength(100)
                .WithMessage("رمز عبور نباید بیشتر از ۱۰۰ کاراکتر باشد");
        }
    }
}
