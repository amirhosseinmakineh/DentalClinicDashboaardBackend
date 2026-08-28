using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Lead
{
    public class GetNewLeadsQueryHandler : IQueryHandler<GetNewLeadsQuery, PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public GetNewLeadsQueryHandler(ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
        }

        public async Task<PaginatedResult<LeadsAssignmentItemsResponse>> HandleAsync(
     GetNewLeadsQuery query,
     CancellationToken cancellationToken = default)
        {
            var newLeads = await leadAssignmentRepository
                .GetAll()
                .AsNoTracking()
                .Where(x =>
                    x.ConsultantProfileId == query.ProfileId &&
                    x.ReportSubmittedAt == null &&
                    x.ReportDescription == null)
                .Select(x => new LeadsAssignmentItemsResponse
                {
                    Id = x.Id,
                    ConsultantProfileId = x.ConsultantProfileId,
                    ReportSubmittedAt = x.ReportSubmittedAt,
                    ReportDescription = x.ReportDescription,
                    UserName = x.UserName,
                    PhoneNumber = x.PhoneNumber,
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<LeadsAssignmentItemsResponse>
            {
                Items = newLeads,
            };
        }
    }
}
