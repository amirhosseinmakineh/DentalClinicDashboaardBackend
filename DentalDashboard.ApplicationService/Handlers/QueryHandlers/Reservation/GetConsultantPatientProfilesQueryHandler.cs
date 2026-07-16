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
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
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

            var totalCount = await profiles.CountAsync(cancellationToken);
            var items = await profiles
                .OrderByDescending(x => x.PatientUser!.PatientProfile!.CreatedAt)
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
                    InsuranceName = x.PatientUser.PatientProfile.InsuranceName,
                    EmergencyPhoneNumber = x.PatientUser.PatientProfile.EmergencyPhoneNumber
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
