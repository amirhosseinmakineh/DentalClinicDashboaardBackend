namespace DentalDashboard.ApplicationService.Services;

using DentalDashboard.Domain.Models;

/// <summary>
/// Hardcoded data for Snapp-style broadcast claim-flow testing.
/// Offline assignment is never filtered by this class.
/// </summary>
internal static class LeadBroadcastTestData
{
    internal static readonly Guid[] TestUserIds =
    [
        Guid.Parse("9B19E167-18F3-4BCC-B10C-706E985A620E"),
        Guid.Parse("E9F685AC-140F-4B0B-ADDB-DBBF407EE669"),
    ];

    internal static readonly (string UserName, string PhoneNumber)[] TestBroadcastLeads =
    [
        ("علی رضایی", "09121111101"),
        ("مریم احمدی", "09121111102"),
        ("حسین کریمی", "09121111103"),
        ("زهرا محمدی", "09121111104"),
        ("رضا نوری", "09121111105"),
    ];

    internal static bool IsTestUser(Guid userId) =>
        TestUserIds.Contains(userId);

    internal static bool IsTestLeadPhone(string phoneNumber) =>
        TestBroadcastLeads.Any(x => x.PhoneNumber == phoneNumber.Trim());

    internal static bool IsTestLead(LeadAssignment lead) =>
        IsTestLeadPhone(lead.PhoneNumber);
}
