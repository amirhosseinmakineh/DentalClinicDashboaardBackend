using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.Secretary;

public sealed class UpdateSecretaryScheduleDto
{
    public SecretaryType SecretaryType { get; set; }
    public List<string> Days { get; set; } = [];
    public List<SecretaryDayPermissionsDto> DayPermissions { get; set; } = [];
}

public sealed class SecretaryDayPermissionsDto
{
    public string Day { get; set; } = string.Empty;
    public List<SecretaryPermissionType> Permissions { get; set; } = [];
}
