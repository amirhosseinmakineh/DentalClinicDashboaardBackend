using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.Tests.Models;

public sealed class ConsultantProfileDeactivationTests
{
    [Fact]
    public void Failed_test_deactivation_disables_cooperation_and_dashboard()
    {
        var consultant = ActiveConsultant();
        consultant.DeactivateCooperation();
        Assert.False(consultant.IsAvailable);
        Assert.False(consultant.IsOnline);
        Assert.False(consultant.User.IsActive);
    }

    [Fact]
    public void Deactivation_is_idempotent()
    {
        var consultant = ActiveConsultant();
        consultant.DeactivateCooperation();
        consultant.DeactivateCooperation();
        Assert.False(consultant.IsAvailable);
        Assert.False(consultant.IsOnline);
        Assert.False(consultant.User.IsActive);
    }

    [Fact]
    public void Successful_test_does_not_deactivate_account()
    {
        var consultant = ActiveConsultant();
        Assert.True(consultant.IsAvailable);
        Assert.True(consultant.IsOnline);
        Assert.True(consultant.User.IsActive);
    }

    private static ConsultantProfile ActiveConsultant() => new()
    {
        IsAvailable = true,
        IsOnline = true,
        User = new User { IsActive = true }
    };
}
