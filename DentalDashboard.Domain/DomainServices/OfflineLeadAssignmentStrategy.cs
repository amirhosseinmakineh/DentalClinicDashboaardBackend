using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.DomainServices
{
    public class OfflineLeadAssignmentStrategy : IOfflineLeadAssignmentStrategy
    {
        public void Assign(IList<LeadAssignment> leads,IList<ConsultantProfile> consultants)
        {
            if (leads == null || !leads.Any())
                return;

            if (consultants == null || !consultants.Any())
                throw new InvalidOperationException("هیچ مشاور آنلاینی برای تخصیص صف آفلاین وجود ندارد.");

            var availableConsultants = consultants
                .Where(x =>
                    !x.IsDeleted &&
                    x.IsCompleteProfile &&
                    x.IsAvailable &&
                    x.IsOnline)
                .OrderBy(x => x.Id)
                .ToList();

            if (!availableConsultants.Any())
                throw new InvalidOperationException("هیچ مشاور آنلاینی برای تخصیص صف آفلاین وجود ندارد.");

            for (int i = 0; i < leads.Count; i++)
            {
                var consultant = availableConsultants[i % availableConsultants.Count];

                leads[i].ConsultantProfileId = consultant.Id;
                leads[i].AssignedAt = DateTime.Now;
                leads[i].LeadAssignmentState = LeadAssignmentState.Assigned;

                leads[i].RequiresThreeMinuteCall = false;
                leads[i].CallDeadlineAt = null;
            }
        }
    }
}
