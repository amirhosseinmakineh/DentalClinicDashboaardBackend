using FluentValidation;

namespace DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser
{
    public class CreateUserCommandValidator
        : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("نام الزامی است.")
                .MaximumLength(100)
                .WithMessage("نام نمیتواند بیشتر از 100 کاراکتر باشد.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("نام خانوادگی الزامی است.")
                .MaximumLength(100)
                .WithMessage("نام خانوادگی نمیتواند بیشتر از 100 کاراکتر باشد.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("شماره موبایل الزامی است.")
                .Matches(@"^09\d{9}$")
                .WithMessage("شماره موبایل معتبر نیست.");

            RuleFor(x => x.PasswordHash)
                .NotEmpty()
                .WithMessage("رمز عبور الزامی است.")
                .MinimumLength(6)
                .WithMessage("رمز عبور باید حداقل 6 کاراکتر باشد.")
                .MaximumLength(100)
                .WithMessage("رمز عبور بیش از حد مجاز است.");

            RuleFor(x => x.BirthDate)
                .NotEmpty()
                .WithMessage("تاریخ تولد الزامی است.")
                .LessThan(DateTime.Now)
                .WithMessage("تاریخ تولد معتبر نیست.");

            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithMessage("جنسیت معتبر نیست.");
        }
    }
}