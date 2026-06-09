namespace DentalDashboard.ApplicationService.Contract.Responses.User
{
    public record CreateUserResponse : BaseResponse<Guid>
    {
        public string RoleName { get; set; } = default!;
        public bool IsActive { get; set; }
    }


}
