using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public sealed record LeadAssignmentCandidateBatch(
    LeadAssignmentSourceType SourceType,
    LeadAssignment? Lead);

public interface ILeadAssignmentCandidateProvider
{
    Task<LeadAssignmentCandidateBatch> GetCurrentForDispatchAsync(
        LeadAssignmentSourceType sourceType,
        TimeSpan redispatchInterval,
        CancellationToken cancellationToken = default);

    Task<LeadAssignmentCandidateBatch> GetActiveAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default);

    Task<LeadAssignmentSourceType> GetSourceTypeAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default);
}
