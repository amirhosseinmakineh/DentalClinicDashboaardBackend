using DentalDashboard.ApplicationService.Contract.Requests.Presence.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Presence;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Presence;

public class GetUserPresenceEventsQueryHandler
    : IQueryHandler<GetUserPresenceEventsQuery, PaginatedResult<UserPresenceEventItemResponse>>
{
    private readonly IUserPresenceLogRepository presenceLogRepository;
    private readonly IAttendanceRepository attendanceRepository;

    public GetUserPresenceEventsQueryHandler(
        IUserPresenceLogRepository presenceLogRepository,
        IAttendanceRepository attendanceRepository)
    {
        this.presenceLogRepository = presenceLogRepository;
        this.attendanceRepository = attendanceRepository;
    }

    public async Task<PaginatedResult<UserPresenceEventItemResponse>> HandleAsync(
        GetUserPresenceEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var (dayStart, dayEnd) = UserPresenceLabels.GetDayRange(query.Date);

        var includePresenceLogs = !query.EventType.HasValue ||
            query.EventType.Value is UserPresenceEventType.Login
                or UserPresenceEventType.Logout
                or UserPresenceEventType.Online
                or UserPresenceEventType.Offline;

        var includeAttendanceEvents = !query.EventType.HasValue ||
            query.EventType.Value is UserPresenceEventType.CheckIn
                or UserPresenceEventType.CheckOut;

        var mergedEvents = new List<(DateTime OccurredAt, UserPresenceEventItemResponse Item)>();

        if (includePresenceLogs)
        {
            var events = presenceLogRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.OccurredAt >= dayStart &&
                            x.OccurredAt <= dayEnd &&
                            x.EventType != UserPresenceEventType.CheckIn &&
                            x.EventType != UserPresenceEventType.CheckOut);

            if (query.UserId.HasValue)
                events = events.Where(x => x.UserId == query.UserId.Value);

            if (query.EventType.HasValue)
                events = events.Where(x => x.EventType == query.EventType.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                events = events.Where(x =>
                    x.User.FirstName.Contains(query.Search) ||
                    x.User.LastName.Contains(query.Search) ||
                    x.User.PhoneNumber.Contains(query.Search));
            }

            var pageLogs = await events
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.User.FirstName,
                    x.User.LastName,
                    x.User.PhoneNumber,
                    RoleName = x.User.UserRoles
                        .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                        .Select(ur => ur.Role!.RoleName)
                        .FirstOrDefault() ?? string.Empty,
                    x.EventType,
                    x.OccurredAt,
                    x.Description
                })
                .ToListAsync(cancellationToken);

            mergedEvents.AddRange(pageLogs.Select(x => (
                x.OccurredAt,
                new UserPresenceEventItemResponse
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    PhoneNumber = x.PhoneNumber,
                    RoleName = x.RoleName,
                    EventType = x.EventType,
                    EventTypeLabel = UserPresenceLabels.ToPersianLabel(x.EventType),
                    OccurredAtPersian = x.OccurredAt.ToPersianDateTimeString(),
                    Description = x.Description
                })));
        }

        if (includeAttendanceEvents)
        {
            var attendances = attendanceRepository.GetAll()
                .Where(x => !x.IsDeleted && x.AttendanceDate == query.Date);

            if (query.UserId.HasValue)
                attendances = attendances.Where(x => x.ConsultantProfile.UserId == query.UserId.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                attendances = attendances.Where(x =>
                    x.ConsultantProfile.User.FirstName.Contains(query.Search) ||
                    x.ConsultantProfile.User.LastName.Contains(query.Search) ||
                    x.ConsultantProfile.User.PhoneNumber.Contains(query.Search));
            }

            var attendanceRows = await attendances
                .Select(x => new
                {
                    x.Id,
                    UserId = x.ConsultantProfile.UserId,
                    x.ConsultantProfile.User.FirstName,
                    x.ConsultantProfile.User.LastName,
                    x.ConsultantProfile.User.PhoneNumber,
                    RoleName = x.ConsultantProfile.User.UserRoles
                        .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                        .Select(ur => ur.Role!.RoleName)
                        .FirstOrDefault() ?? string.Empty,
                    x.AttendanceDate,
                    x.CheckInTime,
                    x.CheckOutTime,
                    x.Description
                })
                .ToListAsync(cancellationToken);

            foreach (var row in attendanceRows)
            {
                if (row.CheckInTime.HasValue &&
                    (!query.EventType.HasValue || query.EventType.Value == UserPresenceEventType.CheckIn))
                {
                    var occurredAt = row.AttendanceDate.ToDateTime(row.CheckInTime.Value);
                    mergedEvents.Add((occurredAt, new UserPresenceEventItemResponse
                    {
                        Id = row.Id * 10 + 1,
                        UserId = row.UserId,
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        PhoneNumber = row.PhoneNumber,
                        RoleName = row.RoleName,
                        EventType = UserPresenceEventType.CheckIn,
                        EventTypeLabel = UserPresenceLabels.ToPersianLabel(UserPresenceEventType.CheckIn),
                        OccurredAtPersian = occurredAt.ToPersianDateTimeString(),
                        Description = row.Description
                    }));
                }

                if (row.CheckOutTime.HasValue &&
                    (!query.EventType.HasValue || query.EventType.Value == UserPresenceEventType.CheckOut))
                {
                    var occurredAt = row.AttendanceDate.ToDateTime(row.CheckOutTime.Value);
                    mergedEvents.Add((occurredAt, new UserPresenceEventItemResponse
                    {
                        Id = row.Id * 10 + 2,
                        UserId = row.UserId,
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        PhoneNumber = row.PhoneNumber,
                        RoleName = row.RoleName,
                        EventType = UserPresenceEventType.CheckOut,
                        EventTypeLabel = UserPresenceLabels.ToPersianLabel(UserPresenceEventType.CheckOut),
                        OccurredAtPersian = occurredAt.ToPersianDateTimeString(),
                        Description = row.Description
                    }));
                }
            }
        }

        var orderedEvents = mergedEvents
            .OrderByDescending(x => x.OccurredAt)
            .ToList();

        var totalCount = orderedEvents.Count;
        var items = orderedEvents
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Item)
            .ToList();

        return new PaginatedResult<UserPresenceEventItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
