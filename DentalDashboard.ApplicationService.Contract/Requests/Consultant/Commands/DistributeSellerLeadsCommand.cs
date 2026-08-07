using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public sealed record DistributeSellerLeadsCommand(long ConsultantId) : ICommand;
