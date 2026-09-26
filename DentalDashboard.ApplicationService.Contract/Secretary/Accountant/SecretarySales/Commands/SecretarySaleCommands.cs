using System.Text.Json.Serialization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;

public sealed class CreateSecretarySaleServiceCommand : ICommand<SecretarySaleServiceDto>
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal SecretaryReward { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateSecretarySaleServiceCommand : ICommand<SecretarySaleServiceDto>
{
    [JsonIgnore] public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal SecretaryReward { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SetSecretarySaleServiceStatusCommand : ICommand
{
    [JsonIgnore] public long Id { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateSecretarySaleCommand : ICommand<SecretarySaleCreatedDto>
{
    [JsonIgnore] public Guid SecretaryUserId { get; set; }
    public Guid PatientUserId { get; set; }
    public long ServiceId { get; set; }
}

public sealed class ApproveSecretarySaleCommand : ICommand
{
    [JsonIgnore] public long SaleId { get; set; }
    [JsonIgnore] public Guid AdminUserId { get; set; }
}

public sealed class RejectSecretarySaleCommand : ICommand
{
    [JsonIgnore] public long SaleId { get; set; }
    [JsonIgnore] public Guid AdminUserId { get; set; }
}

public sealed record SecretarySaleCreatedDto(long SaleId);
