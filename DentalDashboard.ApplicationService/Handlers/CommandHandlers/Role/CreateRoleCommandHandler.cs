using DentalDashboard.ApplicationService.Contract.Requests.Role;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Role
{
    public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand>
    {
        private readonly IRoleRepository roleRepository;

        public CreateRoleCommandHandler(IRoleRepository roleRepository)
        {
            this.roleRepository = roleRepository;
        }

        public async Task<Result> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
        {
            var roles = await roleRepository.GetAllAsync();
            bool checkRoles = roles.Any(x => x.RoleName == command.RoleName);
            if (checkRoles is true)
            {
                return Result<string>.Failure("نقش وارد شده در سیستم موجود میباشد");
            }
            else
            {
                var role = new DentalDashboard.Domain.Models.Role()
                {
                    CreatedAt = DateTime.UtcNow,
                    RoleName = command.RoleName,
                    DeletedAt = null,
                    IsDeleted = false,
                    UpdatedAt = null,
                };
                await roleRepository.AddAsync(role);
                await roleRepository.SaveChange();
                return Result<string>.Success("نقش با موفقیت به سیستم اضافه شد");
            }
        }
    }
}
