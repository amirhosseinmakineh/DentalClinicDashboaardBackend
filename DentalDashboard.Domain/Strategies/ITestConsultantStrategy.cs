namespace DentalDashboard.Domain.Strategies;

public interface ITestConsultantStrategy
{
    TestConsultantDecision Decide(TestConsultantContext context);
}
