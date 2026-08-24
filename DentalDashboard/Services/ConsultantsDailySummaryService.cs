using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed record ConsultantDailySummaryItem(
    long ConsultantProfileId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    int TodayReservationsCount);

public sealed class ConsultantsDailySummaryService
{
    private readonly DentalContext context;

    public ConsultantsDailySummaryService(DentalContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<ConsultantDailySummaryItem>> GetTodaySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) =
            IranTimeHelper.GetIranDayRangeAsUtc(
                IranTimeHelper.TodayInIran());

        var reservationCounts = context.Reservations
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                !x.IsCanceled && 
                x.CreatedAt >= startUtc &&
                x.CreatedAt < endUtc)
            .GroupBy(x => x.ConsultantProfileId)
            .Select(g => new
            {
                ConsultantProfileId = g.Key,
                Count = g.Count()
            });

        var result = await (
            from consultant in context.ConsultantProfiles.AsNoTracking()

            where
                !consultant.IsDeleted &&
                consultant.User != null &&
                consultant.User.UserRoles.Any(ur =>
                    ur.Role != null &&
                    !ur.Role.IsDeleted &&
                    ur.Role.RoleName == "Consultant")

            join reservationCount in reservationCounts
                on consultant.Id equals reservationCount.ConsultantProfileId
                into reservationCountGroup

            from reservationCount in reservationCountGroup.DefaultIfEmpty()

            orderby
                consultant.User!.LastName,
                consultant.User.FirstName

            select new ConsultantDailySummaryItem(
                consultant.Id,
                consultant.User!.FirstName ?? string.Empty,
                consultant.User.LastName ?? string.Empty,
                consultant.User.PhoneNumber ?? string.Empty,
                reservationCount == null
                    ? 0
                    : reservationCount.Count
            )
        ).ToListAsync(cancellationToken);

        return result;
    }

    public Task<int> GetTodayReservationsCountForConsultantAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) =
            IranTimeHelper.GetIranDayRangeAsUtc(
                IranTimeHelper.TodayInIran());

        return context.Reservations
            .AsNoTracking()
            .CountAsync(
                x =>
                    !x.IsDeleted &&
                    !x.IsCanceled &&
                    x.ConsultantProfileId == consultantProfileId &&
                    x.CreatedAt >= startUtc &&
                    x.CreatedAt < endUtc,
                cancellationToken);
    }
}