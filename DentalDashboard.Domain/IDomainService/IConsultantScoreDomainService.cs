using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.IDomainService
{
    public interface IConsultantScoreDomainService
    {
        int DefaultScore { get; }

        int GetCallResultEventScore(LeadCallResult callResult);

        int GetLateCallEventScore();

        int GetReservationAttendanceEventScore(bool approved);

        int CalculateAverageScore(IEnumerable<ScoreLog> scoreLogs);

        void ApplyScoreEvent(ConsultantProfile profile, ScoreLog scoreLog);
    }
}
