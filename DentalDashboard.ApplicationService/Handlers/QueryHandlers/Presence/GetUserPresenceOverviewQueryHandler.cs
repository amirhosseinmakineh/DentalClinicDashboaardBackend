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

public class GetUserPresenceOverviewQueryHandler
    : IQueryHandler<GetUserPresenceOverviewQuery, PaginatedResult<UserPresenceOverviewItemResponse>>
{
    private readonly IUserRepository userRepository;
    private readonly IUserPresenceLogRepository presenceLogRepository;

    public GetUserPresenceOverviewQueryHandler(
        IUserRepository userRepository,
        IUserPresenceLogRepository presenceLogRepository)
    {
        this.userRepository = userRepository;
        this.presenceLogRepository = presenceLogRepository;
    }

    public async Task<PaginatedResult<UserPresenceOverviewItemResponse>> HandleAsync(
        GetUserPresenceOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var (dayStart, dayEnd) = UserPresenceLabels.GetDayRange(query.Date);
        var selectedDatePersian = dayStart.ToPersianDateString();

        var users = userRepository.GetAll()
            .Where(x => !x.IsDeleted)
            .Where(x => x.ConsultantProfile != null && !x.ConsultantProfile.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.FirstName))
            users = users.Where(x => x.FirstName.Contains(query.FirstName));

        if (!string.IsNullOrWhiteSpace(query.LastName))
            users = users.Where(x => x.LastName.Contains(query.LastName));

        if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
            users = users.Where(x => x.PhoneNumber.Contains(query.PhoneNumber));

        if (!string.IsNullOrWhiteSpace(query.RoleName))
        {
            users = users.Where(x => x.UserRoles.Any(ur =>
                !ur.IsDeleted &&
                ur.Role != null &&
                !ur.Role.IsDeleted &&
                ur.Role.RoleName.Contains(query.RoleName)));
        }

        if (query.IsCurrentlyOnline.HasValue)
        {
            var threshold = DateTime.UtcNow - TimeSpan.FromMinutes(5);
            users = query.IsCurrentlyOnline.Value
                ? users.Where(x => x.LastSeenAt.HasValue && x.LastSeenAt.Value >= threshold)
                : users.Where(x => !x.LastSeenAt.HasValue || x.LastSeenAt.Value < threshold);
        }

        var totalCount = await users.CountAsync(cancellationToken);

        var pageUsers = await users
            .OrderByDescending(x => x.LastSeenAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.LastSeenAt,
                RoleName = user.UserRoles
                    .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                    .Select(ur => ur.Role!.RoleName)
                    .FirstOrDefault() ?? string.Empty,
                ConsultantIsOnline = user.ConsultantProfile != null && !user.ConsultantProfile.IsDeleted
                    ? (bool?)user.ConsultantProfile.IsOnline
                    : null,
                ConsultantIsAvailable = user.ConsultantProfile != null && !user.ConsultantProfile.IsDeleted
                    ? (bool?)user.ConsultantProfile.IsAvailable
                    : null
            })
            .ToListAsync(cancellationToken);

        var userIds = pageUsers.Select(x => x.Id).ToList();

        var dayLogs = await presenceLogRepository.GetAll()
            .Where(x => !x.IsDeleted &&
                        userIds.Contains(x.UserId) &&
                        x.OccurredAt >= dayStart &&
                        x.OccurredAt <= dayEnd)
            .ToListAsync(cancellationToken);

        var logsByUser = dayLogs
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var items = pageUsers.Select(user =>
        {
            logsByUser.TryGetValue(user.Id, out var logs);
            logs ??= [];

            DateTime? First(UserPresenceEventType type) =>
                logs.Where(x => x.EventType == type).Select(x => (DateTime?)x.OccurredAt).Min();

            DateTime? Last(UserPresenceEventType type) =>
                logs.Where(x => x.EventType == type).Select(x => (DateTime?)x.OccurredAt).Max();

            return new UserPresenceOverviewItemResponse
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.RoleName,
                IsCurrentlyOnline = UserPresenceLabels.IsCurrentlyOnline(user.LastSeenAt),
                LastSeenAtPersian = user.LastSeenAt?.ToLocalTime().ToPersianDateTimeString(),
                ConsultantIsOnline = user.ConsultantIsOnline,
                ConsultantIsAvailable = user.ConsultantIsAvailable,
                SelectedDatePersian = selectedDatePersian,
                FirstLoginAtPersian = First(UserPresenceEventType.Login)?.ToPersianDateTimeString(),
                LastLogoutAtPersian = Last(UserPresenceEventType.Logout)?.ToPersianDateTimeString(),
                FirstOnlineAtPersian = First(UserPresenceEventType.Online)?.ToPersianDateTimeString(),
                LastOfflineAtPersian = Last(UserPresenceEventType.Offline)?.ToPersianDateTimeString(),
                FirstCheckInAtPersian = First(UserPresenceEventType.CheckIn)?.ToPersianDateTimeString(),
                LastCheckOutAtPersian = Last(UserPresenceEventType.CheckOut)?.ToPersianDateTimeString(),
                EventCountForDay = logs.Count
            };
        }).ToList();

        return new PaginatedResult<UserPresenceOverviewItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
