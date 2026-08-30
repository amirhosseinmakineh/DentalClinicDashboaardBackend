using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using System.Text.Json.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;

public sealed record CreateExpenseCommand: ICommand<CreateExpenseResponse>
{
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    [JsonIgnore]
    public Guid CreatedByUserId { get; set; }
}
public sealed record CreateExpenseResponse(long Id, string Title);

