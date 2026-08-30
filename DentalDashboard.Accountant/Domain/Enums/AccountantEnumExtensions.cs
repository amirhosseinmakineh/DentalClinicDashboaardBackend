using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DentalDashboard.Accountant.Domain.Enums;

public static class AccountantEnumExtensions
{
    public static string GetTitle(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
