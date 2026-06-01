namespace DentalDashboard.ApplicationService.Contract.Responses.Users
{
    public class UserItemResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string RoleName { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}