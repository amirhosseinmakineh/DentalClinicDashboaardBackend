using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ScoreLogResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Queryies
{
    public record GetScoreLogsQuery : IQuery<PaginatedResult<ScoreLogResponse>>
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
