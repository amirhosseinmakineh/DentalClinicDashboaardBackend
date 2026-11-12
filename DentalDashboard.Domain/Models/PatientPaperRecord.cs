using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.Models;

/// <summary>Additional paper-form data, stored independently from existing patient identity and finance.</summary>
public sealed class PatientPaperRecord
{
    public long PatientFileId { get; set; }
    public PatientFile PatientFile { get; set; } = default!;
    public string IntakeJson { get; set; } = "{}";
    public string TreatmentsJson { get; set; } = "[]";
    public string DentalChartJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; }
}
