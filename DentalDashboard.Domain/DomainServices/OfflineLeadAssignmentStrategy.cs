using DentalDashboard.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DentalDashboard.Domain.DomainServices
{
    public class OfflineLeadAssignmentStrategy : IOfflineLeadAssignmentStrategy
    {
        private const int DefaultBatchSize = 5; 
        public void Assign(
            IList<LeadAssignment> leads,
            IList<ConsultantProfile> consultants,
            IReadOnlyDictionary<long, int>? dailyAssignedOfflineLeadCounts = null)
        {
            if (leads == null || !leads.Any())
                return;

            if (consultants == null || !consultants.Any())
                throw new InvalidOperationException("هیچ مشاوری برای تخصیص صف آفلاین وجود ندارد.");

            var availableConsultants = consultants
                .Where(x => !x.IsDeleted && x.IsCompleteProfile && x.IsAvailable)
                .OrderByDescending(x => x.CurrentScore)
                .ThenBy(x => x.Id)
                .ToList();

            if (!availableConsultants.Any())
                throw new InvalidOperationException("هیچ مشاوری برای تخصیص صف آفلاین وجود ندارد.");

            var unassignedLeads = leads
                .Where(l => l.ConsultantProfileId == null)
                .ToList();

            if (!unassignedLeads.Any())
                return;

            int leadIndex = 0;

            var now = DateTime.Now;

            foreach (var consultant in availableConsultants)
            {
                dailyAssignedOfflineLeadCounts?.TryGetValue(consultant.Id, out var alreadyAssignedToday);
                var remainingDailyCapacity = DefaultBatchSize - alreadyAssignedToday;
                if (remainingDailyCapacity <= 0)
                    continue;

                int assignCount = Math.Min(remainingDailyCapacity, unassignedLeads.Count - leadIndex);

                for (int i = 0; i < assignCount; i++)
                {
                    var lead = unassignedLeads[leadIndex];

                    lead.ConsultantProfileId = consultant.Id;
                    lead.AssignedAt = now;
                    lead.LeadAssignmentState = LeadAssignmentState.Assigned;
                    lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                    lead.RequiresThreeMinuteCall = false;
                    lead.CallDeadlineAt = null;

                    leadIndex++;
                }

                if (leadIndex >= unassignedLeads.Count)
                    break;
            }
        }
    }
}