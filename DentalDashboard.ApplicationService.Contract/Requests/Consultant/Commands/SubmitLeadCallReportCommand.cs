using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands
{
    public class SubmitLeadCallReportCommand : ICommand
    {
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public LeadCallResult CallResult { get; set; }
        public string ReportDescription { get; set; } = string.Empty;
    }
}
