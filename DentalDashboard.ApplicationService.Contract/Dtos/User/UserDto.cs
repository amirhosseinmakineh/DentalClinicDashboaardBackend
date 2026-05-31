using DentalDashboard.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalDashboard.ApplicationService.Contract.Dtos.User
{
    public class UserDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string Email { get; set; } = default!;

        public DateTime BirthDate { get; set; }

        public Gender Gender { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
