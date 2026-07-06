using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.DomainServices
{
    public class LeadDomainService : ILeadDomainService
    {
        private static readonly TimeSpan WorkDayStart = TimeSpan.FromHours(9);
        private static readonly TimeSpan WorkDayEnd = TimeSpan.FromHours(21);

        public IEnumerable<LeadAssignment> GetNewLeads(IEnumerable<LeadAssignment> oldNumbers,IEnumerable<LeadAssignment> newNumbers)
        {
            var oldPhoneNumbers = oldNumbers
                .Select(x => x.PhoneNumber)
                .ToHashSet();

            return newNumbers
                .Where(x => !oldPhoneNumbers.Contains(x.PhoneNumber));
        }

        public LeadAssignmentType DetermineAssignmentType(DateTime now, bool hasOnlineConsultant)
        {
            return IsWorkingTime(now) && hasOnlineConsultant
                ? LeadAssignmentType.RealTime
                : LeadAssignmentType.OfflineQueue;
        }

        public bool IsWorkingTime(DateTime now)
        {
            var iranLocalTime = ToIranLocalTime(now);
            return iranLocalTime.TimeOfDay >= WorkDayStart &&
                   iranLocalTime.TimeOfDay < WorkDayEnd;
        }

        public bool IsNightTime(DateTime now)
        {
            return !IsWorkingTime(now);
        }

        internal static DateTime ToIranLocalTime(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
            };

            return TimeZoneInfo.ConvertTimeFromUtc(utc, ResolveIranTimeZone());
        }

        private static TimeZoneInfo ResolveIranTimeZone()
        {
            foreach (var timeZoneId in new[] { "Asia/Tehran", "Iran Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.CreateCustomTimeZone(
                "Iran",
                TimeSpan.FromHours(3.5),
                "Iran",
                "Iran");
        }
    }
}
