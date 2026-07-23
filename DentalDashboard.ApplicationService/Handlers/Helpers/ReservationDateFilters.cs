using DentalDashboard.Domain.Models;
using DentalDashboard.Utilities.Time;

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
            var (startUtc, endUtc) = IranTimeHelper.GetIranDayRangeAsUtc(date.Value);
            return query.Where(x => x.ReservationAt >= startUtc && x.ReservationAt <= endUtc);
        }

        if (from.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(from.Value);
            var (startUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(fromDate);
            query = query.Where(x => x.ReservationAt >= startUtc);
        }

        if (to.HasValue)
        {
            var toDate = DateOnly.FromDateTime(to.Value);
            var (_, endUtc) = IranTimeHelper.GetIranDayRangeAsUtc(toDate);
            query = query.Where(x => x.ReservationAt <= endUtc);
        }

        return query;
    }
}
