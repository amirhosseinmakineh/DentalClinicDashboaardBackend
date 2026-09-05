using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.Commands;

public sealed record UpdateExpenseCategoryCommand : ICommand<ExpenseCategoryResponse>
{
    [JsonIgnore]
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
