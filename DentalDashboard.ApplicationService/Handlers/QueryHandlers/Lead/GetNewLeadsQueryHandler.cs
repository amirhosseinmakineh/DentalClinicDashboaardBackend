using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
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
            var leads = leadAssignmentRepository
                .GetAll()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.ConsultantProfileId == query.ProfileId &&
                    x.LeadAssignmentState != LeadAssignmentState.ClosedByConsultant &&
                    x.ReportSubmittedAt == null &&
                    x.ReportDescription == null);

            if (query.leadAssignmentState.HasValue)
                leads = leads.Where(x => x.LeadAssignmentState == query.leadAssignmentState.Value);
            if (query.LeadAssignmentType.HasValue)
                leads = leads.Where(x => x.AssignmentType == query.LeadAssignmentType.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var search = query.SearchText.Trim();
                leads = leads.Where(x => x.UserName.Contains(search) || x.PhoneNumber.Contains(search) ||
                    (x.SecondaryPhoneNumber != null && x.SecondaryPhoneNumber.Contains(search)));
            }
            if (!string.IsNullOrWhiteSpace(query.UserName))
            {
                var name = query.UserName.Trim();
                leads = leads.Where(x => x.UserName.Contains(name));
            }
            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
            {
                var phone = query.PhoneNumber.Trim();
                leads = leads.Where(x => x.PhoneNumber.Contains(phone) ||
                    (x.SecondaryPhoneNumber != null && x.SecondaryPhoneNumber.Contains(phone)));
            }
            if (!string.IsNullOrWhiteSpace(query.PatientCity))
            {
                var city = query.PatientCity.Trim();
                leads = leads.Where(x => x.PatientCity != null && x.PatientCity.Contains(city));
            }

            leads = leads.ApplyAssignedAtFilter(query);
            var pageNumber = Math.Clamp(query.PageNumber, 1, 1_000_000);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var totalCount = await leads.CountAsync(cancellationToken);
            var newLeads = await leads
                .OrderByDescending(x => x.AssignedAt).ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(x => new LeadsAssignmentItemsResponse
                {
                    Id = x.Id,
                    LeadAssignmentState = x.LeadAssignmentState,
                    leadAssignmentType = x.AssignmentType,
                    ConsultantProfileId = x.ConsultantProfileId,
                    ReportSubmittedAt = x.ReportSubmittedAt,
                    ReportDescription = x.ReportDescription,
                    UserName = x.UserName,
                    PhoneNumber = x.PhoneNumber,
                    SecondaryPhoneNumber = x.SecondaryPhoneNumber,
                    CreatedAt = x.CreatedAt,
                    AssignedAt = x.AssignedAt,
                    CallDeadlineAt = x.CallDeadlineAt,
                    RequiresThreeMinuteCall = x.RequiresThreeMinuteCall,
                    CallInitiatedAt = x.CallInitiatedAt,
                    PatientCity = x.PatientCity,
                    PatientRegion = x.PatientRegion,
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<LeadsAssignmentItemsResponse>
            {
                Items = newLeads,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };
        }
    }
}
