using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class UpdateConsultantLimitNumberCommand : ICommand
{
    public long ProfileId { get; set; }
    public int? LimitNumber { get; set; }
}
