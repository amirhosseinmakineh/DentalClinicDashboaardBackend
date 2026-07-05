using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.DomainServices
{
    public class ConsultantScoreDomainService : IConsultantScoreDomainService
    {
        public int DefaultScore => 100;

        public int GetCallResultEventScore(LeadCallResult callResult) => callResult switch
        {
            LeadCallResult.Converted => 100,
            LeadCallResult.Contacted => 95,
            LeadCallResult.NeedFollowUp => 85,
            LeadCallResult.Busy => 75,
            LeadCallResult.PatientHungUp => 70,
            LeadCallResult.NoAnswer => 65,
            LeadCallResult.Rejected => 55,
            LeadCallResult.WrongNumber => 50,
            _ => 80
        };

        public int GetLateCallEventScore() => 40;

        public int GetReservationAttendanceEventScore(bool approved) => approved ? 95 : 55;

        public int CalculateAverageScore(IEnumerable<ScoreLog> scoreLogs)
        {
            var values = scoreLogs
                .Where(x => !x.IsDeleted)
                .Select(x => x.ScoreValue)
                .ToList();

            if (!values.Any())
                return DefaultScore;

            return (int)Math.Round(values.Average());
        }

        public void ApplyScoreEvent(ConsultantProfile profile, ScoreLog scoreLog)
        {
            profile.ScoreLogs.Add(scoreLog);
            profile.CurrentScore = CalculateAverageScore(profile.ScoreLogs);
        }
    }
}
