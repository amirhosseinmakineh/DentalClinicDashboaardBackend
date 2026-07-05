using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands
{
    public class UpdateLeadCallReportCommand : ICommand<SubmitLeadCallReportResponse>
    {
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public LeadCallResult CallResult { get; set; }
        public string ReportDescription { get; set; } = string.Empty;
        public string? PatientCity { get; set; }
        public string? PatientRegion { get; set; }
        public string? BusinessName { get; set; }
        public int? AttendanceProbabilityPercent { get; set; }
        public string? SecondaryPhoneNumber { get; set; }
    }
}
