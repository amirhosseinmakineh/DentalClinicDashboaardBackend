using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.Secretary;

public sealed class UpdateSecretaryScheduleDto
{
    public SecretaryType SecretaryType { get; set; }
    public List<string> Days { get; set; } = [];
}
