using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.User
{
    public class UserItemResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string RoleName { get; set; } = default!;
        public bool IsActive { get; set; }
        public bool IsCompleteProfile { get; set; }
        public Gender Gender { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
