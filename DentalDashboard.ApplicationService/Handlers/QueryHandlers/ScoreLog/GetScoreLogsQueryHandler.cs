using DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ScoreLogResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.ScoreLog;

public class GetScoreLogsQueryHandler : IQueryHandler<GetScoreLogsQuery, PaginatedResult<ScoreLogResponse>>
{
    private readonly IScoreLogRepository scoreLogRepository;

    public GetScoreLogsQueryHandler(IScoreLogRepository scoreLogRepository)
    {
        this.scoreLogRepository = scoreLogRepository;
    }

    public async Task<PaginatedResult<ScoreLogResponse>> HandleAsync(
        GetScoreLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var scoreLogs = scoreLogRepository.GetAll()
            .Where(scoreLog => !scoreLog.IsDeleted)
            .OrderByDescending(scoreLog => scoreLog.CreatedAt);

        var totalCount = await scoreLogs.CountAsync(cancellationToken);
        var pagedScoreLogs = await scoreLogs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pagedScoreLogs
            .Select(scoreLog => new ScoreLogResponse
            {
                Id = scoreLog.Id,
                ScoreType = ToScoreType(scoreLog.Reason),
                ScoreValue = scoreLog.ScoreValue,
                Description = scoreLog.Description
            })
            .ToList();

        return new PaginatedResult<ScoreLogResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static ScoreType ToScoreType(ScoreReason reason)
    {
        return reason switch
        {
            ScoreReason.ManagerReward => ScoreType.ManagerPositive,
            ScoreReason.ManagerPenalty => ScoreType.ManagerNegative,
            ScoreReason.SuccessfulCall or ScoreReason.FastCall => ScoreType.CallCompleted,
            ScoreReason.FailedCall or ScoreReason.LateCall or ScoreReason.NoAnswer => ScoreType.CallNotCompleted,
            ScoreReason.CustomerPositiveFeedback => ScoreType.UserRatingPositive,
            ScoreReason.CustomerNegativeFeedback => ScoreType.UserRatingNegative,
            _ => ScoreType.CallNotCompleted
        };
    }
}
