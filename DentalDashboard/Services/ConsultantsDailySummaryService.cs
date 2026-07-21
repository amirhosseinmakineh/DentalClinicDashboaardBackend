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

public class ConsultantsDailySummaryService
{
    private readonly DentalContext context;

    public ConsultantsDailySummaryService(DentalContext context) => this.context = context;

    public async Task<IReadOnlyList<ConsultantDailySummaryItem>> GetTodaySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());

        var todayCounts = await context.Reservations.AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsCanceled && x.CreatedAt >= startUtc && x.CreatedAt < endUtc)
            .GroupBy(x => x.ConsultantProfileId)
            .Select(g => new { ConsultantProfileId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConsultantProfileId, x => x.Count, cancellationToken);

        var consultants = await context.ConsultantProfiles.AsNoTracking()
            .Include(x => x.User)
            .ThenInclude(x => x!.UserRoles)
            .ThenInclude(x => x.Role)
            .Where(x => !x.IsDeleted &&
                        x.User != null &&
                        x.User.UserRoles.Any(ur =>
                            ur.Role != null && !ur.Role.IsDeleted && ur.Role.RoleName == "Consultant"))
            .OrderBy(x => x.User!.LastName)
            .ThenBy(x => x.User!.FirstName)
            .ToListAsync(cancellationToken);

        return consultants
            .Select(consultant => new ConsultantDailySummaryItem(
                consultant.Id,
                consultant.User?.FirstName ?? string.Empty,
                consultant.User?.LastName ?? string.Empty,
                consultant.User?.PhoneNumber ?? string.Empty,
                todayCounts.GetValueOrDefault(consultant.Id)))
            .ToList();
    }

    public async Task<int> GetTodayReservationsCountForConsultantAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());

        return await context.Reservations.AsNoTracking()
            .CountAsync(
                x => !x.IsDeleted &&
                     !x.IsCanceled &&
                     x.ConsultantProfileId == consultantProfileId &&
                     x.CreatedAt >= startUtc &&
                     x.CreatedAt < endUtc,
                cancellationToken);
    }
}
