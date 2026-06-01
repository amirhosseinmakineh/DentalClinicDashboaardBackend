using DentalDashboard.ApplicationService.Contract.Requests.Role;
using DentalDashboard.ApplicationService.Contract.Requests.Role.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Role
{
    public class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommaand>
    {
        private readonly IRoleRepository roleRepository;

        public UpdateRoleCommandHandler(IRoleRepository roleRepository)
        {
            this.roleRepository = roleRepository;
        }

        public async Task<Result> HandleAsync(UpdateRoleCommaand command, CancellationToken cancellationToken = default)
        {
            var role = await roleRepository.GetByIdAsync(command.RoleId);
            if (role is not null)
            {
                role.RoleName = command.RoleName;
                roleRepository.Update(role);
                await roleRepository.SaveChange();
                return Result<string>.Success("نقش با موفقیت ویرایش شد");
            }
            else
                return Result<string>.Failure("نقشی یافت نشد");
        }
    }
}
