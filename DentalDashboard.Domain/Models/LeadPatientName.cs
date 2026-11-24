namespace DentalDashboard.Domain.Models;

public sealed record LeadPatientName(string FirstName, string LastName)
{
    public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";

    public static bool TryCreate(string? value, out LeadPatientName? name)
    {
        name = null;
        var normalized = string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0) return false;
        var parts = normalized.Split(' ', 2);
        var lastName = parts.Length > 1 ? parts[1] : string.Empty;
        if (parts[0].Length > 100 || lastName.Length > 100) return false;
        name = new(parts[0], lastName);
        return true;
    }
}
