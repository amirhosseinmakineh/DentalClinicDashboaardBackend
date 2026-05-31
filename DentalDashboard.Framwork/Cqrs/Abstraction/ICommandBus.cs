using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Framwork.Cqrs
{
    public interface ICommandBus
    {
        void Dispatch(ICommand command);
    }


}
