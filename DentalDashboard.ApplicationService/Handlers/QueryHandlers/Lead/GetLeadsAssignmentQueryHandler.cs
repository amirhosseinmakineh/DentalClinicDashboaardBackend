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
        private readonly IReservationRepository reservationRepository;

        public GetLeadsAssignmentQueryHandler(ILeadAssignmentRepository leadAssignmentRepository, IReservationRepository reservationRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<LeadsAssignmentItemsResponse>> HandleAsync(GetLeadsQuery query, CancellationToken cancellationToken = default)
        {
            if (query.LeadAssignmentType == DentalDashboard.Domain.Enums.LeadAssignmentType.RealTime &&
                await HasRealTimeLeadBlockerAsync(query.ProfileId, cancellationToken))
            {
                return new PaginatedResult<LeadsAssignmentItemsResponse>
                {
                    Items = new List<LeadsAssignmentItemsResponse>(),
                    PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber,
                    PageSize = query.PageSize < 1 ? 10 : query.PageSize,
                    TotalCount = 0
                };
            }

            var allLeads = leadAssignmentRepository.GetAll()
                .Where(x=> !x.IsDeleted && x.ConsultantProfileId == query.ProfileId)
                .Select(x => new LeadsAssignmentItemsResponse()
                {
                    Id = x.Id,
                    LeadAssignmentState = x.LeadAssignmentState,
                    leadAssignmentType = x.AssignmentType,
                    PhoneNumber = x.PhoneNumber,
                    UserName = x.UserName,
                    AssignedAt = x.AssignedAt,
                    CallDeadlineAt = x.CallDeadlineAt,
                    RequiresThreeMinuteCall = x.RequiresThreeMinuteCall,
                    HasActiveReservation = reservationRepository.GetAll()
                        .Any(r => r.LeadAssignmentId == x.Id && !r.IsCanceled)
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
        private async Task<bool> HasRealTimeLeadBlockerAsync(long consultantProfileId, CancellationToken cancellationToken)
        {
            var hasDueAttendanceWithoutConsultantDecision = await reservationRepository.GetAll()
                .AnyAsync(x => x.ConsultantProfileId == consultantProfileId &&
                               !x.IsCanceled &&
                               x.ReservationAt <= DateTime.Now &&
                               x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation, cancellationToken);

            if (hasDueAttendanceWithoutConsultantDecision)
                return true;

            return await leadAssignmentRepository.GetAll()
                .AnyAsync(x => !x.IsDeleted &&
                               x.ConsultantProfileId == consultantProfileId &&
                               x.AssignmentType == DentalDashboard.Domain.Enums.LeadAssignmentType.OfflineQueue &&
                               x.ReportSubmittedAt == null &&
                               x.LeadAssignmentState != LeadAssignmentState.Expired &&
                               x.LeadAssignmentState != LeadAssignmentState.Rejected &&
                               x.LeadAssignmentState != LeadAssignmentState.Converted, cancellationToken);
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
