using DentalDashboard.ApplicationService.Contract.Requests.Consultant;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class CompleteConsaltantProfileHandler : ICommandHandler<CompleteConsultantProfileCommand, long>
{
    private readonly IUserRepository userRepository;
    private readonly IConsultantProfileRepository consultantProfileRepository;

    public CompleteConsaltantProfileHandler(
        IUserRepository userRepository,
        IConsultantProfileRepository consultantProfileRepository)
    {
        this.userRepository = userRepository;
        this.consultantProfileRepository = consultantProfileRepository;
    }

    public async Task<Result<long>> HandleAsync(
        CompleteConsultantProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NationalityCode) ||
            command.NationalityCode.Trim().Length != 10 ||
            !command.NationalityCode.Trim().All(char.IsDigit))
        {
            return Result<long>.Failure("کد ملی باید ۱۰ رقم باشد");
        }

        if (string.IsNullOrWhiteSpace(command.Address) || command.Address.Trim().Length < 5)
            return Result<long>.Failure("آدرس مشاور الزامی است");

        ConsultantProfile? profile = null;
        User? user = null;

        if (command.ProfileId > 0)
        {
            profile = await consultantProfileRepository.GetAll()
                .Include(x => x.User)
                .ThenInclude(x => x!.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Id == command.ProfileId && !x.IsDeleted, cancellationToken);

            if (profile is null)
                return Result<long>.Failure("پروفایل مشاور یافت نشد");

            user = profile.User;
        }
        else if (command.UserId != Guid.Empty)
        {
            user = await userRepository.GetAll()
                .Include(x => x.ConsultantProfile)
                .Include(x => x.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Id == command.UserId && !x.IsDeleted, cancellationToken);

            if (user is null)
                return Result<long>.Failure("کاربر یافت نشد");

            profile = user.ConsultantProfile;
        }
        else
        {
            return Result<long>.Failure("شناسه کاربر یا پروفایل مشاور معتبر نیست");
        }

        var activeRoles = user!.UserRoles
            .Where(x => !x.IsDeleted && x.Role != null && !x.Role.IsDeleted)
            .Select(x => x.Role!.RoleName)
            .Distinct()
            .ToList();

        if (!activeRoles.Contains("Consultant"))
        {
            return activeRoles.Contains("Secretary")
                ? Result<long>.Failure("نقش شما به منشی تغییر کرده است. لطفاً دوباره وارد شوید")
                : Result<long>.Failure("این کاربر نقش مشاور ندارد");
        }

        if (profile is null)
        {
            profile = new ConsultantProfile
            {
                Address = command.Address.Trim(),
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null,
                IsAvailable = false,
                IsCompleteProfile = command.IsCompleteProfile,
                IsDeleted = false,
                IsOnline = false,
                LastOfflineAt = null,
                LastOnlineAt = null,
                NationalCode = command.NationalityCode.Trim(),
                Notes = null,
                WorkStartTime = TimeSpan.Zero,
                WorkEndTime = TimeSpan.Zero,
                UserId = user.Id
            };

            await consultantProfileRepository.AddAsync(profile);
        }
        else
        {
            profile.Address = command.Address.Trim();
            profile.NationalCode = command.NationalityCode.Trim();
            profile.IsCompleteProfile = command.IsCompleteProfile;
            profile.UpdatedAt = DateTime.UtcNow;
            consultantProfileRepository.Update(profile);
        }

        user.IsCompleteProfile = command.IsCompleteProfile;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        await consultantProfileRepository.SaveChange();

        return Result<long>.Success(profile.Id, "اطلاعات مشاور با موفقیت تکمیل شد");
    }
}
