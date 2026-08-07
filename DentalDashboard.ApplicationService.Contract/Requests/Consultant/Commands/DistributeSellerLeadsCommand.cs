using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public sealed record DistributeSellerLeadsCommand(IReadOnlyCollection<long> ConsultantIds) : ICommand;
