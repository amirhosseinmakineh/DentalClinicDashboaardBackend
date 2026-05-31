using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.User
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public UserRole Role { get; set; }

        public bool IsActive { get; set; }
    }
}
