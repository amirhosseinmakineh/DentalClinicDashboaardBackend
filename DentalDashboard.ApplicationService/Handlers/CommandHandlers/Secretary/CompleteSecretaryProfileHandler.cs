using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Secretary;

public class CompleteSecretaryProfileHandler : ICommandHandler<CompleteSecretaryProfileCommand, string>
{
    private readonly IUserRepository userRepository;

    public CompleteSecretaryProfileHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<Result<string>> HandleAsync(
        CompleteSecretaryProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Result<string>.Failure("شناسه کاربر معتبر نیست");

        if (string.IsNullOrWhiteSpace(command.NationalityCode) ||
            command.NationalityCode.Trim().Length != 10 ||
            !command.NationalityCode.Trim().All(char.IsDigit))
        {
            return Result<string>.Failure("کد ملی باید ۱۰ رقم باشد");
        }

        if (string.IsNullOrWhiteSpace(command.Address) || command.Address.Trim().Length < 5)
            return Result<string>.Failure("آدرس منشی الزامی است");

        var user = await userRepository.GetAll()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == command.UserId && !x.IsDeleted, cancellationToken);

        if (user is null)
            return Result<string>.Failure("کاربر یافت نشد");

        var activeRoles = user.UserRoles
            .Where(x => !x.IsDeleted && x.Role != null && !x.Role.IsDeleted)
            .Select(x => x.Role!.RoleName)
            .Distinct()
            .ToList();

        if (!activeRoles.Contains("Secretary"))
        {
            return activeRoles.Contains("Consultant")
                ? Result<string>.Failure("نقش شما به مشاور تغییر کرده است. لطفاً دوباره وارد شوید")
                : Result<string>.Failure("این کاربر نقش منشی ندارد");
        }

        user.IsCompleteProfile = command.IsCompleteProfile;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChange();

        return Result<string>.Success(user.Id.ToString(), "اطلاعات منشی با موفقیت تکمیل شد");
    }
}
