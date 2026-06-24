using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
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
            if(query.leadAssignmentState == LeadAssignmentState.New)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.New);
            }
            if(query.LeadAssignmentType == LeadAssignmentType.OfflineQueue)
            {
                allLeads = allLeads.Where(x => x.leadAssignmentType == LeadAssignmentType.OfflineQueue);
            }
            if(query.leadAssignmentState == LeadAssignmentState.Pending)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Pending);
            }
            if(query.leadAssignmentState == LeadAssignmentState.Contacted)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Contacted);
            }
            if(query.leadAssignmentState == LeadAssignmentState.Rejected)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Rejected);
            }
            var result = new PaginatedResult<LeadsAssignmentItemsResponse>()
            {
                Items = allLeads.ToList(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = allLeads.Count()
            };
            return result;
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
            if (query.leadAssignmentState == LeadAssignmentState.New)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.New);
            }
            if (query.LeadAssignmentType == LeadAssignmentType.OfflineQueue)
            {
                allLeads = allLeads.Where(x => x.leadAssignmentType == LeadAssignmentType.OfflineQueue);
            }
            if (query.leadAssignmentState == LeadAssignmentState.Pending)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Pending);
            }
            if (query.leadAssignmentState == LeadAssignmentState.Contacted)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Contacted);
            }
            if (query.leadAssignmentState == LeadAssignmentState.Rejected)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Rejected);
            }
            var result = new PaginatedResult<LeadsAssignmentItemsResponse>()
            {
                Items = allLeads.ToList(),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = allLeads.Count()
            };
            return result;
        }
    }
}
