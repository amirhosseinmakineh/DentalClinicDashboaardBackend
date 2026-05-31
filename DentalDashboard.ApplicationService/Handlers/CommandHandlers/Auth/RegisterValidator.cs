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
                .MaximumLength(100);
            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^09\d{9}$")
                .WithMessage("شماره موبایل معتبر نیست");
            RuleFor(x => x.PasswordHash)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(100);
            RuleFor(x => x.BirthDate)
              .LessThan(DateTime.Now);

            RuleFor(x => x.Gender)
                .IsInEnum();

        }
    }
}
