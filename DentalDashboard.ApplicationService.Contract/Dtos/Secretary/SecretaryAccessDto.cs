using DentalDashboard.Domain.Enums;

public sealed record SecretaryAccessDto(bool IsSecretary,
    SecretaryType? Type,
    IReadOnlyCollection<DayOfWeek> AllowedDays,
    IReadOnlyCollection<SecretaryPermissionType> Permissions)
{
    public bool HasFullAccess => IsSecretary && Type == SecretaryType.Main;
}