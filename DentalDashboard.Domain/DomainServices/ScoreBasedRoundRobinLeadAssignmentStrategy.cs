using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;

namespace DentalDashboard.Domain.DomainServices
{
    public class ScoreBasedRoundRobinLeadAssignmentStrategy : ILeadAssignmentStrategy
    {
        public void Assign(IList<LeadAssignment> leads, IList<ConsultantProfile> consultants)
        {
            if (leads == null || !leads.Any())
                return;

            if (consultants == null || !consultants.Any())
                throw new InvalidOperationException("هیچ مشاور فعالی برای تخصیص لید وجود ندارد.");

            var sortedConsultants = consultants
                .OrderByDescending(x => x.CurrentScore)
                .ThenBy(x => x.Id)
                .ToList();

            for (int i = 0; i < leads.Count; i++)
            {
                var consultant = sortedConsultants[i % sortedConsultants.Count];

                leads[i].ConsultantProfileId = consultant.Id;
                leads[i].AssignedAt = DateTime.Now;
                leads[i].LeadAssignmentState = LeadAssignmentState.Assigned;
                leads[i].AssignmentType = LeadAssignmentType.RealTime;
                leads[i].RequiresThreeMinuteCall = true;
                leads[i].CallDeadlineAt = leads[i].AssignedAt.Value.AddMinutes(3);
            }
        }
    }
}
