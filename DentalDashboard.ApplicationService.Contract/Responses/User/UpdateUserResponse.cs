namespace DentalDashboard.ApplicationService.Contract.Responses.User
{
    public record UpdateUserResponse
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string RoleName { get; set; } = default!;
        public bool IsActive { get; set; }
    }


}
