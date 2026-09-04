using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public sealed record LeadAssignmentCandidateBatch(
    LeadAssignmentSourceType SourceType,
    LeadAssignment? Lead);

public interface ILeadAssignmentCandidateProvider
{
    Task<LeadAssignmentCandidateBatch> GetCurrentForDispatchAsync(
        TimeSpan redispatchInterval,
        CancellationToken cancellationToken = default);

    Task<LeadAssignmentCandidateBatch> GetActiveAsync(
        CancellationToken cancellationToken = default);
}
