using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation
{
    public class GetConsultantPatientProfilesQueryHandler
        : IQueryHandler<GetConsultantPatientProfilesQuery, PaginatedResult<ConsultantPatientProfileItemResponse>>
    {
        private readonly IReservationRepository reservationRepository;

        public GetConsultantPatientProfilesQueryHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<ConsultantPatientProfileItemResponse>> HandleAsync(
            GetConsultantPatientProfilesQuery query,
            CancellationToken cancellationToken = default)
        {
            var requestedPage = query.Page ?? query.PageNumber;
            var pageNumber = requestedPage <= 0 ? 1 : requestedPage;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var profiles = reservationRepository.GetAll()
                .Where(x =>
                    x.ConsultantProfileId == query.ConsultantProfileId &&
                    !x.IsDeleted &&
                    !x.IsCanceled &&
                    x.PatientUserId.HasValue &&
                    x.PatientUser != null &&
                    x.PatientUser.PatientProfile != null);

            if (query.From.HasValue)
            {
                profiles = profiles.Where(x => x.PatientUser!.PatientProfile!.CreatedAt >= query.From.Value);
            }

            if (query.To.HasValue)
            {
                var toExclusive = query.To.Value.Date.AddDays(1);
                profiles = profiles.Where(x => x.PatientUser!.PatientProfile!.CreatedAt < toExclusive);
            }

            var searchText = (query.SearchText ?? query.Search)?.Trim();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                profiles = profiles.Where(x =>
                    (x.PatientUser!.FirstName != null && x.PatientUser.FirstName.Contains(searchText)) ||
                    (x.PatientUser.LastName != null && x.PatientUser.LastName.Contains(searchText)) ||
                    (x.PatientUser.PhoneNumber != null && x.PatientUser.PhoneNumber.Contains(searchText)));
            }

            // A patient may have many reservations. Keep only their newest reservation before
            // counting and paging so TotalCount is a patient count and every page is stable.
            var latestProfiles = profiles.Where(x => !profiles.Any(other =>
                other.PatientUserId == x.PatientUserId &&
                (other.CreatedAt > x.CreatedAt ||
                 (other.CreatedAt == x.CreatedAt && other.Id > x.Id))));

            var totalCount = await latestProfiles.CountAsync(cancellationToken);
            var items = await latestProfiles
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ConsultantPatientProfileItemResponse
                {
                    ReservationId = x.Id,
                    LeadAssignmentId = x.LeadAssignmentId,
                    PatientUserId = x.PatientUserId!.Value,
                    PatientProfileId = x.PatientUser!.PatientProfile!.Id,
                    PatientName = ((x.PatientUser!.FirstName ?? string.Empty) + " " +
                                   (x.PatientUser.LastName ?? string.Empty)).Trim(),
                    PatientPhoneNumber = x.PatientUser.PhoneNumber ?? string.Empty,
                    PatientCity = x.LeadAssignment != null ? x.LeadAssignment.PatientCity : null,
                    PatientRegion = x.LeadAssignment != null ? x.LeadAssignment.PatientRegion : null,
                    ProfileCreatedAt = x.PatientUser.PatientProfile!.CreatedAt,
                    ReservationAt = x.ReservationAt,
                    DoctorName = x.DoctorName
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ConsultantPatientProfileItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
