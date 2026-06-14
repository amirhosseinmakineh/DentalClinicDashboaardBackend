using DentalDashboard.ApplicationService.Contract.Requests.Attendance.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Attendance;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Utilities.Convertor;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Attendance
{
    public class GetConsultantAttendanceCommandHandler : IQueryHandler<GetAttendancesQuery, PaginatedResult<AttendanceResponse>>
    {
        private readonly IAttendanceRepository repository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        public GetConsultantAttendanceCommandHandler(IAttendanceRepository repository, IConsultantProfileRepository consultantProfileRepository)
        {
            this.repository = repository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<PaginatedResult<AttendanceResponse>> HandleAsync(GetAttendancesQuery query, CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository.GetByIdAsync(query.ConsultantProfileId);
            if (profile is not null)
            {
                var attendances = repository.GetAll()
                    .Where(x => x.ConsultantProfileId == query.ConsultantProfileId)
                    .Select(x => new AttendanceResponse()
                    {
                        AttendanceDate = DateConvertor.ToPersianDate(x.AttendanceDate),
                        CheckInTime = DateConvertor.ToPersianTime(x.CheckInTime),
                        CheckOutTime = DateConvertor.ToPersianTime(x.CheckOutTime),
                        Status = x.Status
                    });

                var result = new PaginatedResult<AttendanceResponse>()
                {
                    Items = attendances.ToList(),
                    PageNumber = 1,
                    PageSize = 10,
                    TotalCount = attendances.Count()
                };
                return result;

            }
            else
                throw new Exception("مشاوری یافت نشد");
        }
    }
}
