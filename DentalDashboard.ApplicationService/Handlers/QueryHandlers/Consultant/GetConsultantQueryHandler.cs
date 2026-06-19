using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant
{
    public class GetConsultantQueryHandler : IQueryHandler<GetConsultantQuery, PaginatedResult<ConsultantResponse>>
    {
        private readonly IUserRepository userRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public GetConsultantQueryHandler(
            IUserRepository userRepository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.userRepository = userRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<PaginatedResult<ConsultantResponse>> HandleAsync(
            GetConsultantQuery query,
            CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var baseQuery = userRepository.GetAll()
                .Include(u => u.ConsultantProfile)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u =>
                    u.IsActive &&
                    u.ConsultantProfile != null &&
                    u.ConsultantProfile.IsCompleteProfile &&
                    u.UserRoles.Any(ur => ur.Role.RoleName == "Consultant"));

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                baseQuery = baseQuery.Where(u => u.PhoneNumber == query.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                baseQuery = baseQuery.Where(u => u.FirstName.Contains(query.FirstName));

            if (!string.IsNullOrWhiteSpace(query.LastName))
                baseQuery = baseQuery.Where(u => u.LastName.Contains(query.LastName));

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var consultants = await baseQuery
                .OrderByDescending(u => u.ConsultantProfile.CurrentScore)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new ConsultantResponse
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    ProfileId = u.ConsultantProfile.Id,
                    Id = u.Id
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ConsultantResponse>
            {
                Items = consultants,
                TotalCount = totalCount
            };
        }
    }
}