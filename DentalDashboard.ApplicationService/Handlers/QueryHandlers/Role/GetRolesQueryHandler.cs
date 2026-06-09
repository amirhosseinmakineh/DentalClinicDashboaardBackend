using DentalDashboard.ApplicationService.Contract.Requests.Role.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.RoleResponse;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Role
{
    public class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, PaginatedResult<RoleItemsResponse>>
    {
        private readonly IRoleRepository roleRepository;

        public GetRolesQueryHandler(IRoleRepository roleRepository)
        {
            this.roleRepository = roleRepository;
        }

        public async Task<PaginatedResult<RoleItemsResponse>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var roles = await roleRepository.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(query.RoleName))
                roles = roles.Where(x => x.RoleName == query.RoleName).ToList();

            var totalCount = roles.Count();

            var items = roles
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoleItemsResponse()
                {
                    RoleName = x.RoleName,
                }).ToList();

            return new PaginatedResult<RoleItemsResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
