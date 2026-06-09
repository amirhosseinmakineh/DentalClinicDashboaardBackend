namespace DentalDashboard.ApplicationService.Contract.Responses.User
{
    public record UserResponse : BaseResponse<Guid>
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string RoleName { get; set; } = default!;
        public bool IsActive { get; set; }

    }


}
