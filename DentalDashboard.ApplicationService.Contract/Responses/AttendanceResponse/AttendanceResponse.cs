namespace DentalDashboard.ApplicationService.Contract.Responses.Attendance
{
    public record AttendanceResponse : BaseResponse<long>
    {

        public string AttendanceDate { get; set; } = default!;
        public string CheckInTime { get; set; } = default!;
        public string CheckOutTime { get; set; } = default!;
        public AttendanceStatus Status { get; set; } = default!;
        public string? Description { get; set; } = default!;

    }
}
