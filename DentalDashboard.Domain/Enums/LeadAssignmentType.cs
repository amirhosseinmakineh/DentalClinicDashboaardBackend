namespace DentalDashboard.Domain.Enums
{
    public enum LeadAssignmentType
    {
        RealTime = 1,      // لید زمان کاری و آنلاین؛ قانون 3 دقیقه دارد
        OfflineQueue = 2  // لید خارج زمان کاری یا زمان آفلاین مشاور؛ قانون 3 دقیقه ندارد
    }
}
