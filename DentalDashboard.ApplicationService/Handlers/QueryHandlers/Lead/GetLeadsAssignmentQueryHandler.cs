using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Lead
{
    public class GetLeadsAssignmentQueryHandler : IQueryHandler<GetLeadsQuery, PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public GetLeadsAssignmentQueryHandler(ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
        }

        public async Task<PaginatedResult<LeadsAssignmentItemsResponse>> HandleAsync(GetLeadsQuery query, CancellationToken cancellationToken = default)
        {
            var allLeads = leadAssignmentRepository.GetAll()
                .Where(x=> x.ConsultantProfileId == query.ProfileId)
                .Select(x => new LeadsAssignmentItemsResponse()
                {
                    Id = x.Id,
                    LeadAssignmentState = x.LeadAssignmentState,
                    leadAssignmentType = x.AssignmentType,
                    PhoneNumber = x.PhoneNumber,
                    UserName = x.UserName
                });
            if (query.leadAssignmentState.HasValue)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == query.leadAssignmentState.Value);
            }
            if (query.LeadAssignmentType.HasValue)
            {
                allLeads = allLeads.Where(x => x.leadAssignmentType == query.LeadAssignmentType.Value);
            }

            return await LeadAssignmentPagination.ToPaginatedResultAsync(allLeads, query.PageNumber, query.PageSize, cancellationToken);
        }
    }

    internal static class LeadAssignmentPagination
    {
        public static async Task<PaginatedResult<LeadsAssignmentItemsResponse>> ToPaginatedResultAsync(
            IQueryable<LeadsAssignmentItemsResponse> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
            var normalizedPageSize = pageSize < 1 ? 10 : pageSize;
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<LeadsAssignmentItemsResponse>()
            {
                Items = items,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = totalCount
            };
        }
    }

    public class GetAllLeadsAssignmentQueryHandler : IQueryHandler<GetAllLeadsQuery, PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public GetAllLeadsAssignmentQueryHandler(ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
        }

        public async Task<PaginatedResult<LeadsAssignmentItemsResponse>> HandleAsync(GetAllLeadsQuery query, CancellationToken cancellationToken = default)
        {
            var allLeads = leadAssignmentRepository.GetAll()
                .Select(x => new LeadsAssignmentItemsResponse()
                {
                    Id = x.Id,
                    LeadAssignmentState = x.LeadAssignmentState,
                    leadAssignmentType = x.AssignmentType,
                    PhoneNumber = x.PhoneNumber,
                    UserName = x.UserName
                });
            if (query.leadAssignmentState.HasValue)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == query.leadAssignmentState.Value);
            }
            if (query.LeadAssignmentType.HasValue)
            {
                allLeads = allLeads.Where(x => x.leadAssignmentType == query.LeadAssignmentType.Value);
            }

            return await LeadAssignmentPagination.ToPaginatedResultAsync(allLeads, query.PageNumber, query.PageSize, cancellationToken);
        }
    }
}
