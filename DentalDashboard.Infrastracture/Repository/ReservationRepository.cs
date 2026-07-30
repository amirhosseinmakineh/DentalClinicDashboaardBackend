using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ReservationRepository : BaseRepository<long, Reservation>, IReservationRepository
    {
        public ReservationRepository(DentalContext context) : base(context)
        {
        }

        public Task<int> CountActiveReservationsAtAsync(long consultantProfileId, DateTime reservationAt)
        {
            return GetAll()
                .CountAsync(x => x.ConsultantProfileId == consultantProfileId &&
                                 x.ReservationAt == reservationAt &&
                                 !x.IsCanceled && !x.IsDeleted);
        }

        public Task<int> CountActiveReservationsAtExcludingAsync(
            long consultantProfileId,
            DateTime reservationAt,
            long excludeReservationId)
        {
            return GetAll()
                .CountAsync(x => x.ConsultantProfileId == consultantProfileId &&
                                 x.ReservationAt == reservationAt &&
                                 !x.IsCanceled &&
                                 !x.IsDeleted &&
                                 x.Id != excludeReservationId);
        }

        public Task<bool> HasActiveReservationForLeadAsync(long leadAssignmentId)
        {
            return GetAll()
                .AnyAsync(x => x.LeadAssignmentId == leadAssignmentId && !x.IsCanceled && !x.IsDeleted);
        }
    }
}
