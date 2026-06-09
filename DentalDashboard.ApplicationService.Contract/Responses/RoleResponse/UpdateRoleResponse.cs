namespace DentalDashboard.ApplicationService.Contract.Responses.RoleResponse
{
    public record UpdateRoleResponse : BaseResponse<long>
    {
        public string RoleName { get; set; } = default!;
    }
}
