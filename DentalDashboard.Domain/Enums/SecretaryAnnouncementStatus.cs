using System.Text.Json.Serialization;

namespace DentalDashboard.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SecretaryAnnouncementStatus
{
    NotCalled = 1,
    NoAnswer = 2,
    Confirmed = 3,
    CancelledByPatient = 4,
    RescheduleRequested = 5,
    CallAgain = 6
}
