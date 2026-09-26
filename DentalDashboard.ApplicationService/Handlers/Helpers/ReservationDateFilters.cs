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
            var fromDate = IranTimeHelper.GetDateInIran(from.Value);
            var (startUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(fromDate);
            query = query.Where(x => x.ReservationAt >= startUtc);
        }

        if (to.HasValue)
        {
            var toDate = IranTimeHelper.GetDateInIran(to.Value);
            var (_, endUtc) = IranTimeHelper.GetIranDayRangeAsUtc(toDate);
            query = query.Where(x => x.ReservationAt <= endUtc);
        }

        return query;
    }

    public static IQueryable<Reservation> ApplyCreatedAtFilter(
        this IQueryable<Reservation> query,
        DateOnly? date,
        DateTime? from,
        DateTime? to)
    {
        if (date.HasValue)
        {
            var (startUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(date.Value);
            var (nextDayStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(date.Value.AddDays(1));
            return query.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < nextDayStartUtc);
        }

        if (from.HasValue)
        {
            var fromDate = IranTimeHelper.GetDateInIran(from.Value);
            var (startUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(fromDate);
            query = query.Where(x => x.CreatedAt >= startUtc);
        }

        if (to.HasValue)
        {
            var toDate = IranTimeHelper.GetDateInIran(to.Value);
            var (nextDayStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(toDate.AddDays(1));
            query = query.Where(x => x.CreatedAt < nextDayStartUtc);
        }

        return query;
    }
}
