namespace DentalDashboard.ApplicationService.Contract.Responses.Attendance
{
    public record AttendanceResponse : BaseResponse<long>
    {

        public DateOnly AttendanceDate { get; set; }

        public TimeOnly? CheckInTime { get; set; }

        public TimeOnly? CheckOutTime { get; set; }

        public AttendanceStatus Status { get; set; }
        public string? Description { get; set; }

    }
}
