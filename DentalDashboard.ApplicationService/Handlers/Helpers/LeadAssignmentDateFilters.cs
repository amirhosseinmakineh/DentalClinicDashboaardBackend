using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Handlers.Helpers;

public static class LeadAssignmentDateFilters
{
    public static IQueryable<LeadAssignment> ApplyAssignedAtFilter(
        this IQueryable<LeadAssignment> query,
        GetLeadsQuery leadsQuery)
    {
        var date = leadsQuery.Date;
        var from = leadsQuery.From ?? leadsQuery.FromDate;
        var to = leadsQuery.To ?? leadsQuery.ToDate;

        return query.ApplyAssignedAtFilter(date, from, to);
    }

    public static IQueryable<LeadAssignment> ApplyAssignedAtFilter(
        this IQueryable<LeadAssignment> query,
        DateOnly? date,
        DateTime? from,
        DateTime? to)
    {
        if (date.HasValue)
        {
            return ApplyDayRange(query, date.Value);
        }

        if (!from.HasValue && !to.HasValue)
        {
            return query;
        }

        if (from.HasValue && !to.HasValue)
        {
            return ApplyDayRange(query, DateOnly.FromDateTime(from.Value));
        }

        if (!from.HasValue && to.HasValue)
        {
            return ApplyDayRange(query, DateOnly.FromDateTime(to.Value));
        }

        query = query.Where(x => x.AssignedAt != null && x.AssignedAt >= from!.Value);

        var toExclusive = to!.Value.TimeOfDay == TimeSpan.Zero
            ? to.Value.Date.AddDays(1)
            : to.Value;

        return query.Where(x => x.AssignedAt < toExclusive);
    }

    private static IQueryable<LeadAssignment> ApplyDayRange(
        IQueryable<LeadAssignment> query,
        DateOnly date)
    {
        var (start, end) = UserPresenceLabels.GetDayRange(date);
        return query.Where(x => x.AssignedAt != null && x.AssignedAt >= start && x.AssignedAt <= end);
    }
}
