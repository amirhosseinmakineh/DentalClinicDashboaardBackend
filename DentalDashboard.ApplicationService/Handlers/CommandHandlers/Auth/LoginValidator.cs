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
                .Matches(@"^09\d{9}$")
                .WithMessage("شماره موبایل معتبر نیست");
            RuleFor(x => x.PasswordHash)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(100);

        }
    }
}
