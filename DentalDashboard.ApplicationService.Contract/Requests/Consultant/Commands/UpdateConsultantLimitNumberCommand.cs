using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class UpdateConsultantLimitNumberCommand : ICommand<ConsultantLimitUpdateResponse>
{
    public long ProfileId { get; set; }
    public int? LimitNumber { get; set; }
}
