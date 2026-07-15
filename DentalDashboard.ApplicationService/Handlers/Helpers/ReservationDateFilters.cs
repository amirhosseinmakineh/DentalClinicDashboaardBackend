using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Handlers.Helpers;

public static class ReservationDateFilters
{
    public static IQueryable<Reservation> ApplyReservationAtFilter(
        this IQueryable<Reservation> query,
        DateOnly? date,
        DateTime? from,
        DateTime? to)
    {
        if (date.HasValue)
        {
            var (start, end) = UserPresenceLabels.GetDayRange(date.Value);
            return query.Where(x => x.ReservationAt >= start && x.ReservationAt <= end);
        }

        if (from.HasValue)
            query = query.Where(x => x.ReservationAt >= from.Value);

        if (to.HasValue)
        {
            var toExclusive = to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.Date.AddDays(1)
                : to.Value;

            query = query.Where(x => x.ReservationAt < toExclusive);
        }

        return query;
    }
}
