using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Commands
{
    public class ScoreLogCommand : ICommand
    {
        public long ConsultantProfileId { get; set; }

        public ScoreSource Source { get; set; }

        public ScoreReason Reason { get; set; }

        public int ScoreValue { get; set; }

        public string? Description { get; set; }

        public long? LeadAssignmentId { get; set; }

        public Guid? CreatedByUserId { get; set; }
    }
}
