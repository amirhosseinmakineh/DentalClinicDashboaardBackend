namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;

public record AdminConsultantProfileResponse
{
    public long ProfileId { get; init; }
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string PhoneNumber { get; init; } = default!;
    public bool UserIsActive { get; init; }
    public bool UserIsCompleteProfile { get; init; }
    public string NationalCode { get; init; } = default!;
    public string Address { get; init; } = default!;
    public bool IsAvailable { get; init; }
    public bool IsOnline { get; init; }
    public bool IsCompleteProfile { get; init; }
    public TimeSpan WorkStartTime { get; init; }
    public TimeSpan WorkEndTime { get; init; }
    public string? Notes { get; init; }
    public DateTime? LastOnlineAt { get; init; }
    public DateTime? LastOfflineAt { get; init; }
    public int? LimitNumber { get; init; }
    public int EffectiveDailyLimit { get; init; }
    public int TodayPickupCount { get; init; }
}
