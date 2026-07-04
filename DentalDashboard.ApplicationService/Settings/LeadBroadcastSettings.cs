namespace DentalDashboard.ApplicationService.Settings;

public sealed class LeadBroadcastSettings
{
    public const string SectionName = "LeadBroadcast";

    public int TimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// When set, real-time broadcast is limited to these consultant UserIds (for testing).
    /// Offline queue assignment is never filtered.
    /// </summary>
    public List<string> TestUserIds { get; set; } =
    [
        "9B19E167-18F3-4BCC-B10C-706E985A620E",
        "E9F685AC-140F-4B0B-ADDB-DBBF407EE669",
    ];
}
