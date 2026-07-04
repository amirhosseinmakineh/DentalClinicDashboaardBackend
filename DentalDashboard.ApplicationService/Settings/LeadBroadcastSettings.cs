namespace DentalDashboard.ApplicationService.Settings;

public sealed class LeadBroadcastSettings
{
    public const string SectionName = "LeadBroadcast";

    public int TimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// When true, hardcoded test broadcast leads are only visible to TestUserIds.
    /// Production broadcast leads remain visible to all online consultants.
    /// </summary>
    public bool EnableTestBroadcastLeads { get; set; } = true;

    public List<string> TestUserIds { get; set; } =
    [
        "9B19E167-18F3-4BCC-B10C-706E985A620E",
        "E9F685AC-140F-4B0B-ADDB-DBBF407EE669",
    ];
}
