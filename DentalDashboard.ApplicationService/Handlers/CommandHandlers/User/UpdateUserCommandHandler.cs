using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.ApplicationService.Contract.Responses.User;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand,UpdateUserResponse>
    {
        private readonly IUserRepository userRepository;
        private readonly IUnitOfWork unitOfWork;
        private readonly IRoleService roleService;
        private readonly IConsultantProfileService consultantProfileService;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public UpdateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IUserRepository userRepository,
            IRoleService roleService,
            IConsultantProfileService consultantProfileService,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.unitOfWork = unitOfWork;
            this.userRepository = userRepository;
            this.roleService = roleService;
            this.consultantProfileService = consultantProfileService;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result<UpdateUserResponse>> HandleAsync(
            UpdateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await userRepository.GetByIdAsync(command.Id);

                if (user == null)
                {
                    await unitOfWork.RollbackAsync();
                    return Result<UpdateUserResponse>.Failure("کاربر یافت نشد");
                }

                user.FirstName = command.FirstName;
                user.LastName = command.LastName;
                user.PhoneNumber = command.PhoneNumber;
                user.IsCompleteProfile = command.IsCompleteProfile;
                user.AvatarImageName = command.AvatarImageName;
                user.Gender = command.Gender;
                user.IsActive = command.IsActive;
                user.SecretaryType = string.Equals(command.RoleName, "Secretary", StringComparison.OrdinalIgnoreCase)
                    ? command.SecretaryType ?? DentalDashboard.Domain.Enums.SecretaryType.Main
                    : null;

                userRepository.Update(user);

                var currentRoleName = await userRepository.GetAll()
                    .Where(x => x.Id == user.Id)
                    .SelectMany(x => x.UserRoles)
                    .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                    .OrderByDescending(ur => ur.UpdatedAt)
                    .ThenByDescending(ur => ur.Id)
                    .Select(ur => ur.Role!.RoleName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.Equals(currentRoleName, command.RoleName, StringComparison.Ordinal))
                {
                    await roleService.SetUserRole(user.Id, command.RoleName);

                    if (command.RoleName == "Consultant")
                    {
                        var profileId = await consultantProfileService.EnsureProfileExistsAsync(user.Id);
                        user.IsCompleteProfile = profileId.HasValue && await consultantProfileRepository
                            .GetAll()
                            .AnyAsync(profile => profile.Id == profileId.Value &&
                                !profile.IsDeleted && profile.IsCompleteProfile,
                                cancellationToken);
                        if (!user.IsCompleteProfile)
                            user.IsActive = false;
                    }
                }

                await userRepository.SaveChange();
                await unitOfWork.CommitAsync();
                var response = new UpdateUserResponse()
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    RoleName = command.RoleName
                };
                return Result<UpdateUserResponse>.Success(response,"ویرایش کاربر با موفقیت انجام شد");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                return Result<UpdateUserResponse>.Failure($"خطا در ویرایش کاربر: {ex.Message}");
            }
        }
    }
}
