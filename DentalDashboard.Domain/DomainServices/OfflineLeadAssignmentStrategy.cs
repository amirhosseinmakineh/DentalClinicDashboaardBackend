using DentalDashboard.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DentalDashboard.Domain.DomainServices
{
    public class OfflineLeadAssignmentStrategy : IOfflineLeadAssignmentStrategy
    {
        private const int DefaultBatchSize = 5; 
        public void Assign(IList<LeadAssignment> leads, IList<ConsultantProfile> consultants)
        {
            if (leads == null || !leads.Any())
                return;

            if (consultants == null || !consultants.Any())
                throw new InvalidOperationException("هیچ مشاوری برای تخصیص صف آفلاین وجود ندارد.");

            var availableConsultants = consultants
                .Where(x => !x.IsDeleted && x.IsCompleteProfile && x.IsAvailable)
                .OrderBy(x => x.Id)
                .ToList();

            if (!availableConsultants.Any())
                throw new InvalidOperationException("هیچ مشاوری برای تخصیص صف آفلاین وجود ندارد.");

            var unassignedLeads = leads
                .Where(l => l.ConsultantProfileId == null)
                .ToList();

            if (!unassignedLeads.Any())
                return;

            int leadIndex = 0;

            foreach (var consultant in availableConsultants)
            {
                int assignCount = Math.Min(DefaultBatchSize, unassignedLeads.Count - leadIndex);

                for (int i = 0; i < assignCount; i++)
                {
                    var lead = unassignedLeads[leadIndex];

                    lead.ConsultantProfileId = consultant.Id;
                    lead.AssignedAt = DateTime.Now;
                    lead.LeadAssignmentState = LeadAssignmentState.Assigned;
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