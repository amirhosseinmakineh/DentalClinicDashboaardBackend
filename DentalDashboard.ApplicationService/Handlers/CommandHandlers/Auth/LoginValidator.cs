using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using FluentValidation;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth
{
    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
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
        }
    }
}
