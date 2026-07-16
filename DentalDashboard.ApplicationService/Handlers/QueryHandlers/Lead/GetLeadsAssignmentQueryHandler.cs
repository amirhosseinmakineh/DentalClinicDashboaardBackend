using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Lead
{
    public class GetLeadsAssignmentQueryHandler : IQueryHandler<GetLeadsQuery, PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IReservationRepository reservationRepository;

        public GetLeadsAssignmentQueryHandler(ILeadAssignmentRepository leadAssignmentRepository, IReservationRepository reservationRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<LeadsAssignmentItemsResponse>> HandleAsync(GetLeadsQuery query, CancellationToken cancellationToken = default)
        {
            var leadsQuery = leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted && x.ConsultantProfileId == query.ProfileId);

            if (query.leadAssignmentState.HasValue)
            {
                leadsQuery = leadsQuery.Where(x => x.LeadAssignmentState == query.leadAssignmentState.Value);
            }

            if (query.LeadAssignmentType.HasValue)
            {
                leadsQuery = leadsQuery.Where(x => x.AssignmentType == query.LeadAssignmentType.Value);
            }

            if (query.HasSubmittedReport.HasValue)
            {
                leadsQuery = query.HasSubmittedReport.Value
                    ? leadsQuery.Where(x => x.ReportSubmittedAt != null)
                    : leadsQuery.Where(x => x.ReportSubmittedAt == null);
            }

            leadsQuery = leadsQuery.ApplyAssignedAtFilter(query.Date, query.From, query.To);

            var allLeads = leadsQuery.Select(x => new LeadsAssignmentItemsResponse()
            {
                Id = x.Id,
                LeadAssignmentState = x.LeadAssignmentState,
                leadAssignmentType = x.AssignmentType,
                PhoneNumber = x.PhoneNumber,
                UserName = x.UserName,
                AssignedAt = x.AssignedAt,
                CallDeadlineAt = x.CallDeadlineAt,
                RequiresThreeMinuteCall = x.RequiresThreeMinuteCall,
                IsReportSubmitted = x.ReportSubmittedAt != null,
                ReportSubmittedAt = x.ReportSubmittedAt,
                ContactedAt = x.ContactedAt,
                CallInitiatedAt = x.CallInitiatedAt,
                CallResult = x.CallResult,
                ReportDescription = x.ReportDescription,
                PatientCity = x.PatientCity,
                PatientRegion = x.PatientRegion,
                BusinessName = x.BusinessName,
                AttendanceProbabilityPercent = x.AttendanceProbabilityPercent,
                SecondaryPhoneNumber = x.SecondaryPhoneNumber,
                HasActiveReservation = reservationRepository.GetAll()
                    .Any(r => r.LeadAssignmentId == x.Id && !r.IsCanceled)
            });

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
                .Where(x => !x.IsDeleted)
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
