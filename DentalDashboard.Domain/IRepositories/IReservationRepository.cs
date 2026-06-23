using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IReservationRepository : IBaseRepository<long, Reservation>
    {
        Task<int> CountActiveReservationsAtAsync(long consultantProfileId, DateTime reservationAt);
        Task<bool> HasActiveReservationForLeadAsync(long leadAssignmentId);
    }
}
