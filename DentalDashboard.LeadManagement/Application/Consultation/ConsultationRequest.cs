using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace DentalDashboard.LeadManagement.Application.Consultation;

public sealed class ConsultationRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, StringLength(30)] public string PhoneNumber { get; set; } = "";
    [Required, MinLength(1), MaxLength(10)] public long[] ServiceIds { get; set; } = [];
    [StringLength(1000)] public string? Description { get; set; }
}

public static class ConsultationInput
{
    public static string NormalizeName(string value) => Regex.Replace(value.Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();

    public static bool IsValidName(string value) =>
        value.Count(char.IsLetter) >= 3 &&
        Regex.IsMatch(value, @"^[\p{L}\p{M} '\-\u200c]+$") && value.Length <= 100;

    public static string NormalizePhone(string value)
    {
        var digits = new string(value.Trim().Select(c => c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') :
            c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c).ToArray());
        digits = Regex.Replace(digits, @"[\s()\-]", "");
        if (digits.StartsWith("+98")) digits = "0" + digits[3..];
        else if (digits.StartsWith("0098")) digits = "0" + digits[4..];
        else if (digits.StartsWith("98") && digits.Length == 12) digits = "0" + digits[2..];
        else if (digits.StartsWith('9') && digits.Length == 10) digits = "0" + digits;
        return digits;
    }

    public static bool IsValidPhone(string value) => Regex.IsMatch(value, "^09[0-9]{9}$");
}
