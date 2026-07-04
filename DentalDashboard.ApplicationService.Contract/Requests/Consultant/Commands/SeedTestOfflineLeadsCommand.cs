using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class SeedTestOfflineLeadsCommand : ICommand<SeedTestOfflineLeadsResponse>
{
    public int Count { get; set; } = 5;
}
