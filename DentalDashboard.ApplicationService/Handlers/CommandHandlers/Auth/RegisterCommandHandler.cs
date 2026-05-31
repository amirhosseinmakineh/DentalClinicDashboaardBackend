using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Hasher;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth
{
    public class RegisterCommandHandler : ICommandHandler<RegisterCommand, object>
    {
        private readonly IUserRepository userRepository;
        private readonly IRoleService roleService;
        private readonly IUnitOfWork unitOfWork;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IRoleService roleService,
            IUnitOfWork unitOfWork)
        {
            this.userRepository = userRepository;
            this.roleService = roleService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Result<object>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            var exists = await userRepository.ExistsAsync(
                x => x.PhoneNumber == command.PhoneNumber);

            if (exists)
            {
                return Result<object>.Failure(
                    "کاربری با این شماره موبایل قبلاً ثبت شده است");
            }

            var user = new Domain.Models.User
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                PhoneNumber = command.PhoneNumber,
                PasswordHash = PasswordHasher.HashPassword(command.PasswordHash),
                BirthDate = command.BirthDate,
                Gender = command.Gender,
                AvatarImageName = command.AvatarImageName,
                IsActive = true,
                IsCompleteProfile = false
            };

            await userRepository.AddAsync(user);

            await roleService.AddRoleToUser(user.Id, "NormalUser");

            await unitOfWork.CommitAsync();

            return Result<object>.Success(user.Id);
        }
    }
}