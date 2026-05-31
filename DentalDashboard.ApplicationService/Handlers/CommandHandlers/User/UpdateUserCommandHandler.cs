using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.User
{
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IUserRepository userRepository;
        private readonly IUserRoleRepository userRoleRepository;
        private readonly IUnitOfWork unitOfWork;

        public UpdateUserCommandHandler(IUserRoleRepository userRoleRepository, IUnitOfWork unitOfWork, IUserRepository userRepository)
        {
            this.userRoleRepository = userRoleRepository;
            this.unitOfWork = unitOfWork;
            this.userRepository = userRepository;
        }
        public async Task<Result> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            await unitOfWork.BeginTransactionAsync();

            var user = await userRepository.GetByIdAsync(command.Id);
            if (user == null)
            {
                return Result<object>.Failure("کاربر یافت نشد");
            }

            user.FirstName = command.FirstName;
            user.LastName = command.LastName;
            user.PhoneNumber = command.PhoneNumber;
            user.IsCompleteProfile = command.IsCompleteProfile;
            user.AvatarImageName = command.AvatarImageName;
            user.Gender = command.Gender;
            user.IsActive = command.IsActive;

             userRepository.Update(user);

            await unitOfWork.CommitAsync();

            return Result<object>.Success(user,"ویرایش کاربر با موفقیت انجام شد");
        }
    }
}