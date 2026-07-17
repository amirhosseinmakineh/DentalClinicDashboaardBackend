using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant
{
    public class GetConsultantQueryHandler : IQueryHandler<GetConsultantQuery, PaginatedResult<ConsultantResponse>>
    {
        private readonly IUserRepository userRepository;
        private readonly IAttendanceRepository attendanceRepository;

        public GetConsultantQueryHandler(
            IUserRepository userRepository,
            IAttendanceRepository attendanceRepository)
        {
            this.userRepository = userRepository;
            this.attendanceRepository = attendanceRepository;
        }

        public async Task<PaginatedResult<ConsultantResponse>> HandleAsync(
            GetConsultantQuery query,
            CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var baseQuery = userRepository.GetAll()
                .Include(u => u.ConsultantProfile)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u =>
                    u.IsActive &&
                    u.ConsultantProfile != null &&
                    u.ConsultantProfile.IsCompleteProfile &&
                    u.UserRoles.Any(ur => ur.Role.RoleName == "Consultant"));

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                baseQuery = baseQuery.Where(u => u.PhoneNumber == query.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(query.FirstName))
                baseQuery = baseQuery.Where(u => u.FirstName.Contains(query.FirstName));

            if (!string.IsNullOrWhiteSpace(query.LastName))
                baseQuery = baseQuery.Where(u => u.LastName.Contains(query.LastName));

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var consultants = await baseQuery
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new ConsultantResponse
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    ProfileId = u.ConsultantProfile.Id,
                    Id = u.Id,
                    ConsultantIsOnline = u.ConsultantProfile.IsOnline
                })
                .ToListAsync(cancellationToken);

            var profileIds = consultants.Select(x => x.ProfileId).ToList();

            if (profileIds.Count > 0)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var todayAttendances = await attendanceRepository.GetAll()
                    .Where(x => !x.IsDeleted &&
                                profileIds.Contains(x.ConsultantProfileId) &&
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

                foreach (var consultant in consultants)
                {
                    if (!attendanceByProfile.TryGetValue(consultant.ProfileId, out var attendance))
                    {
                        consultant.ConsultantIsAvailable = false;
                        continue;
                    }

                    consultant.ConsultantIsAvailable = AttendanceLabels.IsCurrentlyPresent(
                        attendance.CheckInTime,
                        attendance.CheckOutTime);
                }
            }

            return new PaginatedResult<ConsultantResponse>
            {
                Items = consultants,
                TotalCount = totalCount
            };
        }
    }
}
