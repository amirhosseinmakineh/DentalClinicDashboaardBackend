using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IReservationRepository : IBaseRepository<long, Reservation>
    {
        Task<int> CountActiveReservationsAtAsync(long consultantProfileId, DateTime reservationAt);
        Task<int> CountActiveReservationsAtExcludingAsync(
            long consultantProfileId,
            DateTime reservationAt,
            long excludeReservationId);
        Task<bool> HasActiveReservationForLeadAsync(long leadAssignmentId);
    }
}
