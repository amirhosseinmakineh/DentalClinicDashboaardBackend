using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.Consultant
{
    public class ConsultantDto
    {
        public Guid UserId { get; set; }
        public bool IsAvailable { get; set; } = false;
        public TimeSpan WorkStartTime { get; set; }
        public TimeSpan WorkEndTime { get; set; }
        public string? Notes { get; set; }
        public bool IsCompleteProfile { get; set; }
        public bool IsOnline { get; set; }
        public int? LimitNumber { get; set; }
        public ConsultantRole ConsultantRole { get; set; }
    }
}
