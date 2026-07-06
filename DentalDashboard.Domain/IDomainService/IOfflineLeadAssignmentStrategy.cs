using DentalDashboard.Domain.Models;

public interface IOfflineLeadAssignmentStrategy
{
    void Assign(
        IList<LeadAssignment> leads,
        IList<ConsultantProfile> consultants,
        IReadOnlyDictionary<long, int>? pendingOfflineLeadCounts = null);
}
