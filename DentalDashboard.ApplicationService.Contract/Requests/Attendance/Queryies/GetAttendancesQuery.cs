using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Attendance;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Attendance.Queryies
{
    public record GetAttendancesQuery : IQuery<PaginatedResult<AttendanceResponse>>
    {
        public long ConsultantProfileId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
