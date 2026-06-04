public interface IOfflineLeadAssignmentStrategy
{
    void Assign(IList<LeadAssignment> leads,IList<ConsultantProfile> consultants);
}