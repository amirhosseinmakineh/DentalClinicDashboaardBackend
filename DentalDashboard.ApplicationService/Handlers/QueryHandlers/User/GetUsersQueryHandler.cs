using DentalDashboard.ApplicationService.Contract.Requests.User.Queries.User;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.User;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.User
{
    public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PaginatedResult<UserItemResponse>>
    {
        private readonly IUserRepository userRepository;
        private readonly IAttendanceRepository attendanceRepository;

        public GetUsersQueryHandler(
            IUserRepository userRepository,
            IAttendanceRepository attendanceRepository)
        {
            this.userRepository = userRepository;
            this.attendanceRepository = attendanceRepository;
        }

        public async Task<PaginatedResult<UserItemResponse>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var users = userRepository.GetAll()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                users = users.Where(x => x.FirstName.Contains(query.FirstName));

            if (!string.IsNullOrWhiteSpace(query.LastName))
                users = users.Where(x => x.LastName.Contains(query.LastName));

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                users = users.Where(x => x.PhoneNumber.Contains(query.PhoneNumber));

            if (!string.IsNullOrWhiteSpace(query.RoleName))
                users = users.Where(x => x.UserRoles.Any(ur =>
                    !ur.IsDeleted &&
                    ur.Role != null &&
                    !ur.Role.IsDeleted &&
                    ur.Role.RoleName.Contains(query.RoleName)));

            if (query.Gender.HasValue)
                users = users.Where(x => x.Gender == query.Gender.Value);

            if (query.IsActive.HasValue)
                users = users.Where(x => x.IsActive == query.IsActive.Value);

            if (query.IsCompleteName.HasValue)
                users = users.Where(x => x.IsCompleteProfile == query.IsCompleteName.Value);

            var totalCount = await users.CountAsync(cancellationToken);

            var items = await users
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new UserItemResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    IsCompleteProfile = user.IsCompleteProfile,
                    Gender = user.Gender,
                    CreatedAt = user.CreatedAt,
                    PhoneNumber = user.PhoneNumber,
                    LastSeenAt = user.LastSeenAt,
                    RoleName = user.UserRoles
                        .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
                        .OrderByDescending(ur => ur.UpdatedAt)
                        .ThenByDescending(ur => ur.Id)
                        .Select(ur => ur.Role!.RoleName)
                        .FirstOrDefault() ?? string.Empty,
                    ConsultantProfileId = user.ConsultantProfile != null && !user.ConsultantProfile.IsDeleted
                        ? (long?)user.ConsultantProfile.Id
                        : null,
                    ConsultantIsOnline = user.ConsultantProfile != null && !user.ConsultantProfile.IsDeleted
                        ? (bool?)user.ConsultantProfile.IsOnline
                        : null
                })
                .ToListAsync(cancellationToken);

            var consultantProfileIds = items
                .Where(x => x.ConsultantProfileId.HasValue)
                .Select(x => x.ConsultantProfileId!.Value)
                .ToList();

            if (consultantProfileIds.Count > 0)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var todayAttendances = await attendanceRepository.GetAll()
                    .Where(x => !x.IsDeleted &&
                                consultantProfileIds.Contains(x.ConsultantProfileId) &&
                                x.AttendanceDate == today)
                    .Select(x => new
                    {
                        x.ConsultantProfileId,
                        x.CheckInTime,
                        x.CheckOutTime
                    })
                    .ToListAsync(cancellationToken);

                var attendanceByProfile = todayAttendances
                    .GroupBy(x => x.ConsultantProfileId)
                    .ToDictionary(x => x.Key, x => x.First());

                foreach (var item in items)
                {
                    if (!item.ConsultantProfileId.HasValue)
                        continue;

                    if (!attendanceByProfile.TryGetValue(item.ConsultantProfileId.Value, out var attendance))
                    {
                        item.ConsultantIsAvailable = false;
                        continue;
                    }

                    item.ConsultantIsAvailable = AttendanceLabels.IsCurrentlyPresent(
                        attendance.CheckInTime,
                        attendance.CheckOutTime);
                }
            }

            return new PaginatedResult<UserItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
