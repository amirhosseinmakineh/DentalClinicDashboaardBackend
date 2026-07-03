namespace DentalDashboard.ApplicationService.Contract.Dtos.User
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string RoleName { get; set; } = default!;

        public bool IsActive { get; set; }
    }
}
