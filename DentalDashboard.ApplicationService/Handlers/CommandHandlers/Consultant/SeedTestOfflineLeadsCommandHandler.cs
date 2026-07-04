using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class SeedTestOfflineLeadsCommandHandler
    : ICommandHandler<SeedTestOfflineLeadsCommand, SeedTestOfflineLeadsResponse>
{
    private readonly ILeadAssignmentService leadAssignmentService;

    public SeedTestOfflineLeadsCommandHandler(ILeadAssignmentService leadAssignmentService)
    {
        this.leadAssignmentService = leadAssignmentService;
    }

    public async Task<Result<SeedTestOfflineLeadsResponse>> HandleAsync(
        SeedTestOfflineLeadsCommand command,
        CancellationToken cancellationToken = default)
    {
        var count = command.Count is < 1 or > 20 ? 5 : command.Count;
        var created = await leadAssignmentService.SeedTestOfflineLeadsAsync(count);

        return Result<SeedTestOfflineLeadsResponse>.Success(
            new SeedTestOfflineLeadsResponse { CreatedCount = created },
            $"{created} لید آفلاین تست به صف اضافه شد");
    }
}
