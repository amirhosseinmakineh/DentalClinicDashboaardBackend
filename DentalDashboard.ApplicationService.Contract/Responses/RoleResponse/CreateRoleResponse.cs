namespace DentalDashboard.ApplicationService.Contract.Responses.RoleResponse
{
    public record CreateRoleResponse : BaseResponse<long>
    {
        public string RoleName { get; set; }  = default!;
    }
}
