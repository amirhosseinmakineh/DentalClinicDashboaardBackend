using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.User
{
    public class UpdateUserDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public DateTime BirthDate { get; set; }

        public Gender Gender { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; }
    }
}
