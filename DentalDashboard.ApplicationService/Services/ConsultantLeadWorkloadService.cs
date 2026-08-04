using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services;

public sealed class ConsultantLeadWorkloadService : IConsultantLeadWorkloadService
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;

    public ConsultantLeadWorkloadService(ILeadAssignmentRepository leadAssignmentRepository)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
    }

    public async Task<ConsultantLeadWorkloadStatus> GetStatusAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var leads = leadAssignmentRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.ConsultantProfileId == consultantProfileId);

        var uncalledWithoutReportCount = await leads.CountAsync(
            x => x.ReportSubmittedAt == null &&
                 x.LeadAssignmentState != LeadAssignmentState.Expired &&
                 x.LeadAssignmentState != LeadAssignmentState.Rejected,
            cancellationToken);

        var followUpCount = await leads.CountAsync(
            x => x.ReportSubmittedAt != null &&
                 x.CallResult == LeadCallResult.NeedFollowUp &&
                 x.LeadAssignmentState == LeadAssignmentState.Pending,
            cancellationToken);

        return new ConsultantLeadWorkloadStatus
        {
            UncalledWithoutReportCount = uncalledWithoutReportCount,
            FollowUpCount = followUpCount
        };
    }
}
