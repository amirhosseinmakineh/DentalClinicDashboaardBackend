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

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                leadsQuery = leadsQuery.Where(x =>
                    x.UserName.Contains(searchText) ||
                    x.PhoneNumber.Contains(searchText) ||
                    (x.SecondaryPhoneNumber != null && x.SecondaryPhoneNumber.Contains(searchText)));
            }

            if (!string.IsNullOrWhiteSpace(query.UserName))
            {
                var userName = query.UserName.Trim();
                leadsQuery = leadsQuery.Where(x => x.UserName.Contains(userName));
            }

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
            {
                var phoneNumber = query.PhoneNumber.Trim();
                leadsQuery = leadsQuery.Where(x =>
                    x.PhoneNumber.Contains(phoneNumber) ||
                    (x.SecondaryPhoneNumber != null && x.SecondaryPhoneNumber.Contains(phoneNumber)));
            }

            if (!string.IsNullOrWhiteSpace(query.PatientCity))
            {
                var patientCity = query.PatientCity.Trim();
                leadsQuery = leadsQuery.Where(x =>
                    x.PatientCity != null && x.PatientCity.Contains(patientCity));
            }

            leadsQuery = leadsQuery.ApplyAssignedAtFilter(query);

            var allLeads = leadsQuery.Select(x => new LeadsAssignmentItemsResponse()
            {
                Id = x.Id,
                LeadAssignmentState = x.LeadAssignmentState,
                leadAssignmentType = x.AssignmentType,
                PhoneNumber = x.PhoneNumber,
                UserName = x.UserName,
                CreatedAt = x.CreatedAt,
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
                ConsultantProfileId = x.ConsultantProfileId,
                AttendanceProbabilityPercent = x.AttendanceProbabilityPercent,
                SecondaryPhoneNumber = x.SecondaryPhoneNumber,
                HasActiveReservation = reservationRepository.GetAll()
                    .Any(r => r.LeadAssignmentId == x.Id)
            });

            return await LeadAssignmentPagination.ToPaginatedResultAsync(allLeads,cancellationToken);
        }
    }

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

    internal static class LeadAssignmentPagination
    {
        public static async Task<PaginatedResult<LeadsAssignmentItemsResponse>> ToPaginatedResultAsync(
            IQueryable<LeadsAssignmentItemsResponse> query,
            CancellationToken cancellationToken)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.AssignedAt.HasValue)
                .ThenByDescending(x => x.AssignedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<LeadsAssignmentItemsResponse>()
            {
                Items = items,
                TotalCount = totalCount
            };
        }
    }

    public class GetAllLeadsAssignmentQueryHandler : IQueryHandler<GetAllLeadsQuery, PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IReservationRepository reservationRepository;

        public GetAllLeadsAssignmentQueryHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IReservationRepository reservationRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<LeadsAssignmentItemsResponse>> HandleAsync(GetAllLeadsQuery query, CancellationToken cancellationToken = default)
        {
            var leadsQuery = leadAssignmentRepository.GetAll().Where(x => !x.IsDeleted);

            if (query.ReservationOptionsOnly)
            {
                leadsQuery = leadsQuery.Where(x =>
                    x.ReportSubmittedAt.HasValue &&
                    (x.CallResult == LeadCallResult.Contacted || x.CallResult == LeadCallResult.Converted) &&
                    !reservationRepository.GetAll().Any(r =>
                        !r.IsDeleted && !r.IsCanceled && r.LeadAssignmentId == x.Id));
            }

            var allLeads = leadsQuery
                .Select(x => new LeadsAssignmentItemsResponse()
                {
                    Id = x.Id,
                    LeadAssignmentState = x.LeadAssignmentState,
                    leadAssignmentType = x.AssignmentType,
                    PhoneNumber = x.PhoneNumber,
                    UserName = x.UserName,
                    ConsultantProfileId = x.ConsultantProfile != null &&
                                          !x.ConsultantProfile.IsDeleted &&
                                          x.ConsultantProfile.IsCompleteProfile &&
                                          !x.ConsultantProfile.User.IsDeleted &&
                                          x.ConsultantProfile.User.IsActive
                        ? x.ConsultantProfileId
                        : null,
                    ConsultantFullName = x.ConsultantProfile == null
                        ? null
                        : x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
                    ConsultantPhoneNumber = x.ConsultantProfile == null
                        ? null
                        : x.ConsultantProfile.User.PhoneNumber,
                    CreatedAt = x.CreatedAt,
                    AssignedAt = x.AssignedAt,
                    ContactedAt = x.ContactedAt,
                    ReportSubmittedAt = x.ReportSubmittedAt,
                });
            if (query.leadAssignmentState.HasValue)
            {
                allLeads = allLeads.Where(x => x.LeadAssignmentState == query.leadAssignmentState.Value);
            }
            if (query.LeadAssignmentType.HasValue)
            {
                allLeads = allLeads.Where(x => x.leadAssignmentType == query.LeadAssignmentType.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                allLeads = allLeads.Where(x =>
                    x.UserName.Contains(searchText) || x.PhoneNumber.Contains(searchText));
            }

            return await LeadAssignmentPagination.ToPaginatedResultAsync(allLeads, cancellationToken);
        }
    }
}
