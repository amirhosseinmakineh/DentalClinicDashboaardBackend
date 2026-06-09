using DentalDashboard.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse
{
    public class LeadsAssignmentItemsResponse
    {
        public string UserName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public LeadAssignmentState LeadAssignmentState { get; set; }
        public LeadAssignmentType leadAssignmentType { get; set; }    }
}
