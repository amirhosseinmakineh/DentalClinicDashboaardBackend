using FluentValidation;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands
{
    public class CreateConsultantPatientLeadCommandValidator
        : AbstractValidator<CreateConsultantPatientLeadCommand>
    {
        public CreateConsultantPatientLeadCommandValidator()
        {
            RuleFor(x => x.ConsultantProfileId)
                .GreaterThan(0)
                .WithMessage("شناسه مشاور معتبر نیست");

            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage("نام بیمار الزامی است")
                .MaximumLength(200)
                .WithMessage("نام بیمار بیش از حد مجاز است");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("شماره موبایل الزامی است")
                .Matches(@"^09\d{9}$")
                .WithMessage("شماره موبایل معتبر نیست");

            RuleFor(x => x.SecondaryPhoneNumber)
                .Matches(@"^09\d{9}$")
                .When(x => !string.IsNullOrWhiteSpace(x.SecondaryPhoneNumber))
                .WithMessage("شماره تماس دوم معتبر نیست");

            RuleFor(x => x.PatientCity)
                .MaximumLength(100)
                .WithMessage("شهر بیمار بیش از حد مجاز است");

            RuleFor(x => x.PatientRegion)
                .MaximumLength(100)
                .WithMessage("منطقه بیمار بیش از حد مجاز است");
        }
    }
}
