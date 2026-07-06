using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DentalDashboard.Domain.DomainServices
{
    public class OfflineLeadAssignmentStrategy : IOfflineLeadAssignmentStrategy
    {
        public const int OfflineBatchSize = 5;

        public void Assign(
            IList<LeadAssignment> leads,
            IList<ConsultantProfile> consultants,
            IReadOnlyDictionary<long, int>? pendingOfflineLeadCounts = null)
        {
            if (leads == null || !leads.Any())
                return;

            if (consultants == null || !consultants.Any())
                throw new InvalidOperationException("هیچ مشاوری برای تخصیص صف آفلاین وجود ندارد.");

            var availableConsultants = consultants
                .Where(c => !c.IsDeleted && c.IsCompleteProfile && c.IsAvailable)
                .OrderByDescending(c => c.CurrentScore)
                .ThenBy(c => c.Id)
                .ToList();

            if (!availableConsultants.Any())
                throw new InvalidOperationException("هیچ مشاوری برای تخصیص صف آفلاین وجود ندارد.");

            var unassignedLeads = leads
                .Where(l => l.ConsultantProfileId == null)
                .ToList();

            if (!unassignedLeads.Any())
                return;

            var now = DateTime.Now;
            var leadIndex = 0;

            foreach (var consultant in availableConsultants)
            {
                if (leadIndex >= unassignedLeads.Count)
                    break;

                var pendingCount = GetPendingOfflineCount(pendingOfflineLeadCounts, consultant.Id);
                var remainingCapacity = OfflineBatchSize - pendingCount;

                if (remainingCapacity <= 0)
                    continue;

                var assignCount = Math.Min(
                    remainingCapacity,
                    unassignedLeads.Count - leadIndex);

                for (var i = 0; i < assignCount; i++)
                {
                    AssignLeadToConsultant(
                        unassignedLeads[leadIndex],
                        consultant.Id,
                        now);

                    leadIndex++;
                }
            }
        }

        private static int GetPendingOfflineCount(
            IReadOnlyDictionary<long, int>? pendingOfflineLeadCounts,
            long consultantId)
        {
            if (pendingOfflineLeadCounts == null)
                return 0;

            return pendingOfflineLeadCounts.TryGetValue(consultantId, out var count)
                ? count
                : 0;
        }

        private static void AssignLeadToConsultant(
            LeadAssignment lead,
            long consultantId,
            DateTime assignedAt)
        {
            lead.ConsultantProfileId = consultantId;
            lead.AssignedAt = assignedAt;
            lead.LeadAssignmentState = LeadAssignmentState.Assigned;
            lead.AssignmentType = LeadAssignmentType.OfflineQueue;
            lead.RequiresThreeMinuteCall = false;
            lead.CallDeadlineAt = null;
        }
    }
}
