using DentalDashboard.ApplicationService.Contract.Requests.Attendance.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Attendance;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Attendance
{
    public class GetConsultantAttendanceCommandHandler : IQueryHandler<GetAttendancesQuery, PaginatedResult<AttendanceResponse>>
    {
        private readonly IAttendanceRepository repository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public GetConsultantAttendanceCommandHandler(
            IAttendanceRepository repository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.repository = repository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<PaginatedResult<AttendanceResponse>> HandleAsync(
            GetAttendancesQuery query,
            CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository.GetByIdAsync(query.ConsultantProfileId);
            if (profile is null)
                throw new Exception("مشاوری یافت نشد");

            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var attendancesQuery = repository.GetAll()
                .Where(x => !x.IsDeleted && x.ConsultantProfileId == query.ConsultantProfileId)
                .OrderByDescending(x => x.AttendanceDate)
                .ThenByDescending(x => x.CheckInTime);

            var totalCount = await attendancesQuery.CountAsync(cancellationToken);

            var items = await attendancesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AttendanceResponse
                {
                    Id = x.Id,
                    AttendanceDate = DateConvertor.ToPersianDate(x.AttendanceDate),
                    CheckInTime = x.CheckInTime.HasValue
                        ? DateConvertor.ToPersianTime(x.CheckInTime.Value)
                        : string.Empty,
                    CheckOutTime = x.CheckOutTime.HasValue
                        ? DateConvertor.ToPersianTime(x.CheckOutTime.Value)
                        : string.Empty,
                    Status = x.Status,
                    Description = x.Description
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<AttendanceResponse>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
