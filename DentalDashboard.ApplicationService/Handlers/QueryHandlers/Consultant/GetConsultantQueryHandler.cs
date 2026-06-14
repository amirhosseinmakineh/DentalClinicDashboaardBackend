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

        public GetConsultantQueryHandler(IUserRepository userRepository, IConsultantProfileRepository consultantProfileRepository)
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
                .Include(c => c.ConsultantProfile)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(x => x.IsActive &&
                            x.ConsultantProfile.IsCompleteProfile == true &&
                            x.ConsultantProfile != null &&
                            x.UserRoles.Any(ur => ur.Role.RoleName == "Consultant"));

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                baseQuery = baseQuery.Where(x => x.PhoneNumber == query.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                baseQuery = baseQuery.Where(x => x.FirstName.Contains(query.FirstName));

            if (!string.IsNullOrWhiteSpace(query.LastName))
                baseQuery = baseQuery.Where(x => x.LastName.Contains(query.LastName));

            consultants = consultants.OrderByDescending(x =>
                x.ConsultantProfile.CurrentScore);
            var totalCount = await consultants.CountAsync(cancellationToken);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var consultants = await baseQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return new PaginatedResult<ConsultantResponse>
            {
                Items = baseQuery.Select(x => new ConsultantResponse()
                {
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    PhoneNumber = x.PhoneNumber,
                    ProfileId = x.ConsultantProfile.Id,
                    Id = x.Id
                }).ToList(),
                TotalCount = totalCount
            };
        }
    }
}
