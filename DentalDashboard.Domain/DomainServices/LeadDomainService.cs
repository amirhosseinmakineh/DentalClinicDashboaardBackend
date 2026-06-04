using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.DomainServices
{
    public class LeadDomainService : ILeadDomainService
    {
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
            var isWorkingTime =
                now.TimeOfDay >= TimeSpan.FromHours(9) &&
                now.TimeOfDay < TimeSpan.FromHours(21);

            return isWorkingTime && hasOnlineConsultant
                ? LeadAssignmentType.RealTime
                : LeadAssignmentType.OfflineQueue;
        }
    }
}
