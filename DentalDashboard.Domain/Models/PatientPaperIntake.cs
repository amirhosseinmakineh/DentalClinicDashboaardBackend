namespace DentalDashboard.Domain.Models;

/// <summary>
/// Data collected from the clinic's paper patient intake form.
/// This is an additive contract; it does not generate or modify PatientFile.FileNumber.
/// Persistence and API integration must be added before using it for patient records.
/// </summary>
public sealed class PatientPaperIntake
{
    public DateOnly? BirthDate { get; set; }
    public string? MaritalStatus { get; set; }
    public string? EducationLevel { get; set; }
    public string? HomePhone { get; set; }
    public string? HomeAddress { get; set; }
    public string? WorkAddress { get; set; }
    public string? ReferralSource { get; set; }
    public PatientPaperMedicalHistory MedicalHistory { get; set; } = new();
    public string? DentalNotes { get; set; }
    public bool? ConsentAccepted { get; set; }
    public DateTime? ConsentDate { get; set; }
    public string? PatientSignature { get; set; }
}

/// <summary>
/// Nullable conditions distinguish 'not answered' from 'no'.
/// </summary>
public sealed class PatientPaperMedicalHistory
{
    public bool? HeartDisease { get; set; }
    public bool? BloodPressure { get; set; }
    public bool? Bleeding { get; set; }
    public bool? Diabetes { get; set; }
    public bool? Thyroid { get; set; }
    public bool? LiverDisease { get; set; }
    public bool? RespiratoryDisease { get; set; }
    public bool? KidneyDisease { get; set; }
    public bool? Hepatitis { get; set; }
    public bool? Malignancy { get; set; }
    public bool? InfectiousDisease { get; set; }
    public bool? DigestiveDisease { get; set; }
    public bool? Allergy { get; set; }
    public bool? Rheumatism { get; set; }
    public bool? Epilepsy { get; set; }
    public string? CurrentMedications { get; set; }
    public string? HospitalizationHistory { get; set; }
    public string? DrugAllergies { get; set; }
    public string? PregnancyOrBreastfeeding { get; set; }
    public string? AdditionalMedicalNotes { get; set; }
}
