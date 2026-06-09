using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant
{
    public class CompleteConsultantProfileCommand : ICommand<long>
    {
        public long ProfileId { get; set; }
        public string NationalityCode { get; set; } = default!;
        public string Address { get; set; } = default!;
        public bool IsCompleteProfile { get; set; }
    }
}
