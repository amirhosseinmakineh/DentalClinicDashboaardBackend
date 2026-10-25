namespace DentalDashboard.Domain.Enums;

public static class DentalServiceTypeExtensions
{
    public static string ToPersian(this DentalServiceType service) => service switch
    {
        DentalServiceType.Composite       => "کامپوزیت",
        DentalServiceType.Implant         => "ایمپلنت",
        DentalServiceType.Laminate        => "لمینت",
        DentalServiceType.Crown           => "روکش",
        DentalServiceType.RootCanal       => "عصب کشی",
        DentalServiceType.Filling         => "ترمیم",
        DentalServiceType.ToothExtraction => "کشیدن دندان",
        _                                 => service.ToString()
    };
}
