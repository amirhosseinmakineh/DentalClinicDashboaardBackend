using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Handlers.Helpers;

public static class LeadAssignmentDateFilters
{
    public static IQueryable<LeadAssignment> ApplyAssignedAtFilter(
        this IQueryable<LeadAssignment> query,
        DateOnly? date,
        DateTime? from,
        DateTime? to)
    {
        if (date.HasValue)
        {
            var (start, end) = UserPresenceLabels.GetDayRange(date.Value);
            return query.Where(x => x.AssignedAt != null && x.AssignedAt >= start && x.AssignedAt <= end);
        }

        if (!from.HasValue && !to.HasValue)
            return query;

        if (from.HasValue)
            query = query.Where(x => x.AssignedAt != null && x.AssignedAt >= from.Value);

        if (to.HasValue)
        {
            var toExclusive = to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.Date.AddDays(1)
                : to.Value;

            query = query.Where(x => x.AssignedAt != null && x.AssignedAt < toExclusive);
        }

        return query;
    }
}
