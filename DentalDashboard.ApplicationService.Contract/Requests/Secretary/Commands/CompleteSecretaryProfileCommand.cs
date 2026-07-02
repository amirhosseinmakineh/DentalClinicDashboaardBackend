using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;

public class CompleteSecretaryProfileCommand : ICommand<string>
{
    public Guid UserId { get; set; }

    public string NationalityCode { get; set; } = default!;

    public string Address { get; set; } = default!;

    public bool IsCompleteProfile { get; set; }
}
