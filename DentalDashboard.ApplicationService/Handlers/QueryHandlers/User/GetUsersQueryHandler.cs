using DentalDashboard.ApplicationService.Contract.Requests.User.Queries.User;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.User;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.User
{
    public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PaginatedResult<UserItemResponse>>
    {
        private readonly IUserRepository userRepository;

        public GetUsersQueryHandler(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public async Task<PaginatedResult<UserItemResponse>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var users = await userRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                users = users.Where(x => x.FirstName.Contains(query.FirstName)).ToList();

            if (!string.IsNullOrWhiteSpace(query.LastName))
                users = users.Where(x => x.LastName.Contains(query.LastName)).ToList();

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                users = users.Where(x => x.PhoneNumber.Contains(query.PhoneNumber)).ToList();

            if (!string.IsNullOrWhiteSpace(query.RoleName))
                users = users.Where(x => x.UserRoles.Any(ur => ur.Role.RoleName.Contains(query.RoleName))).ToList();

            if (query.Gender.HasValue)
                users = users.Where(x => x.Gender == query.Gender.Value).ToList();

            if (query.IsActive.HasValue)
                users = users.Where(x => x.IsActive == query.IsActive.Value).ToList();

            if (query.IsCompleteName.HasValue)
                users = users.Where(x => x.IsCompleteProfile == query.IsCompleteName.Value).ToList();

            var totalCount = users.Count();

            var items = users
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new UserItemResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    PhoneNumber = user.PhoneNumber,
                    RoleName = user.UserRoles
                        .Select(userRole => userRole.Role.RoleName)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToList();

            return new PaginatedResult<UserItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}