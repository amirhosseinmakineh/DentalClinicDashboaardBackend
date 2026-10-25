using DentalDashboard.ApplicationService.Contract.Requests.Role;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Role
{
    public class DeleteRoleCommandHnadler : ICommandHandler<DeleteRoleCommaand>
    {
        private readonly IRoleRepository roleRepository;

        public DeleteRoleCommandHnadler(IRoleRepository roleRepository)
        {
            this.roleRepository = roleRepository;
        }

        public async Task<Result> HandleAsync(DeleteRoleCommaand command, CancellationToken cancellationToken = default)
        {
            var role = await roleRepository.GetByIdAsync(command.RoleId);
            if (role is not null)
            {
                role.IsDeleted = true;
                roleRepository.Update(role);
                await roleRepository.SaveChange();
                return Result<string>.Success("نقش با موفقیت حذف شد");
            }
            else
                return Result<string>.Failure("نقشی یافت نششد");
        }
    }
}
