using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;

public sealed record GetActiveSellerConsultantsQuery : IQuery<IReadOnlyList<ActiveSellerConsultantResponse>>;
