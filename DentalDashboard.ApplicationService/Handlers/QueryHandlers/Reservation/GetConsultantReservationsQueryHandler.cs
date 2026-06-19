using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation
{
    public class GetConsultantReservationsQueryHandler : IQueryHandler<GetConsultantReservationsQuery, PaginatedResult<ReservationItemResponse>>
    {
        private readonly IReservationRepository reservationRepository;

        public GetConsultantReservationsQueryHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<PaginatedResult<ReservationItemResponse>> HandleAsync(GetConsultantReservationsQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var reservations = reservationRepository.GetAll()
                .Where(x => x.ConsultantProfileId == query.ConsultantProfileId);

            if (!query.IncludeCanceled)
                reservations = reservations.Where(x => !x.IsCanceled);

            if (query.From.HasValue)
                reservations = reservations.Where(x => x.ReservationAt >= query.From.Value);

            if (query.To.HasValue)
                reservations = reservations.Where(x => x.ReservationAt <= query.To.Value);

            var totalCount = await reservations.CountAsync(cancellationToken);
            var items = await reservations
                .Include(x => x.LeadAssignment)
                .OrderBy(x => x.ReservationAt)
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ReservationItemResponse
                {
                    Id = x.Id,
                    LeadAssignmentId = x.LeadAssignmentId,
                    ConsultantProfileId = x.ConsultantProfileId,
                    ReservationAt = x.ReservationAt,
                    PatientName = x.LeadAssignment.UserName,
                    PatientPhoneNumber = x.LeadAssignment.PhoneNumber,
                    Description = x.Description,
                    IsCanceled = x.IsCanceled
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ReservationItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
