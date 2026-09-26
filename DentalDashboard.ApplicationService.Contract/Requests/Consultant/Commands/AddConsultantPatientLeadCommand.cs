using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands
{
    public class AddConsultantPatientLeadCommand : ICommand<AddConsultantPatientLeadResponse>
    {
        public long ConsultantProfileId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? PatientCity { get; set; }
        public string? PatientRegion { get; set; }
        public string? SecondaryPhoneNumber { get; set; }
        public string? ReportDescription { get; set; }
    }
}
