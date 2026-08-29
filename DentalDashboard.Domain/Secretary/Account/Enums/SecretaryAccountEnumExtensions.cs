using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DentalDashboard.Domain.Secretary.Account.Enums;

public static class SecretaryAccountEnumExtensions
{
    public static string GetTitle(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
