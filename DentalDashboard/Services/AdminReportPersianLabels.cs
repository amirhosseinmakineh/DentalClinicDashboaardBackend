using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Services;

public static class AdminReportPersianLabels
{
    public static string ToPersian(this Gender gender) => gender switch
    {
        Gender.Male => "مرد",
        Gender.Female => "زن",
        _ => "نامشخص"
    };

    public static string ToPersianRole(string? roleName) => roleName switch
    {
        "Admin" => "ادمین",
        "Consultant" => "مشاور",
        "Secretary" => "منشی",
        "Patient" => "بیمار",
        "User" => "کاربر",
        "NormalUser" => "کاربر",
        null or "" => "بدون نقش",
        _ => roleName
    };

    public static string ToPersian(this LeadAssignmentState state) => state switch
    {
        LeadAssignmentState.New => "جدید",
        LeadAssignmentState.Assigned => "تخصیص‌یافته",
        LeadAssignmentState.Contacted => "تماس گرفته شده",
        LeadAssignmentState.Pending => "پیگیری",
        LeadAssignmentState.Converted => "تبدیل شده",
        LeadAssignmentState.Expired => "منقضی شده",
        LeadAssignmentState.Rejected => "رد شده",
        _ => "نامشخص"
    };

    public static string ToPersian(this LeadCallResult result) => result switch
    {
        LeadCallResult.Contacted => "تماس برقرار شد",
        LeadCallResult.Converted => "تبدیل به رزرو",
        LeadCallResult.Rejected => "رد شد",
        LeadCallResult.NoAnswer => "پاسخ نداد",
        LeadCallResult.WrongNumber => "شماره اشتباه",
        LeadCallResult.NeedFollowUp => "نیاز به پیگیری",
        LeadCallResult.Busy => "اشغال",
        LeadCallResult.PatientHungUp => "قطع تماس توسط بیمار",
        _ => "نامشخص"
    };

    public static string ToPersian(this LeadAssignmentType type) => type switch
    {
        LeadAssignmentType.RealTime => "آنی",
        LeadAssignmentType.OfflineQueue => "صف آفلاین",
        LeadAssignmentType.ConsultantPatient => "بیمار مشاور",
        _ => "نامشخص"
    };

    public static string ToYesNo(bool value) => value ? "بله" : "خیر";

    public static string ToAssignmentStatus(long? consultantProfileId) =>
        consultantProfileId.HasValue ? "اساین شده" : "اساین نشده";

    public static string ToCallStatus(bool hasCalled) =>
        hasCalled ? "تماس گرفته" : "تماس نگرفته";

    public static string ToYesNoNullable(bool? value) =>
        value.HasValue ? ToYesNo(value.Value) : "ثبت نشده";

    public static string ToPersian(this ReservationAttendanceConfirmationStatus status) => status switch
    {
        ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation => "منتظر اعلام مشاور",
        ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent => "مشاور: بیمار آمده",
        ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent => "مشاور: بیمار نیامده",
        ReservationAttendanceConfirmationStatus.SecretaryApproved => "تایید نهایی منشی",
        ReservationAttendanceConfirmationStatus.SecretaryRejected => "رد نهایی منشی",
        _ => "نامشخص"
    };
}
