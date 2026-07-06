using DentalDashboard.ApplicationService.Contract.Requests.Presence.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Presence;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Presence;

public class GetUserPresenceEventsQueryHandler
    : IQueryHandler<GetUserPresenceEventsQuery, PaginatedResult<UserPresenceEventItemResponse>>
{
    private readonly IUserPresenceLogRepository presenceLogRepository;

    public GetUserPresenceEventsQueryHandler(IUserPresenceLogRepository presenceLogRepository)
    {
        this.presenceLogRepository = presenceLogRepository;
    }

    public async Task<PaginatedResult<UserPresenceEventItemResponse>> HandleAsync(
        GetUserPresenceEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var (dayStart, dayEnd) = UserPresenceLabels.GetDayRange(query.Date);

        var events = presenceLogRepository.GetAll()
            .Where(x => !x.IsDeleted &&
                        x.OccurredAt >= dayStart &&
                        x.OccurredAt <= dayEnd);

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

        var totalCount = await events.CountAsync(cancellationToken);

        var pageLogs = await events
            .OrderByDescending(x => x.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        var items = pageLogs.Select(x => new UserPresenceEventItemResponse
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
        }).ToList();

        return new PaginatedResult<UserPresenceEventItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
