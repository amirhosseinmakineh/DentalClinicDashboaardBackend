using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Framwork.Cqrs
{
    public class CommandBus : ICommandBus
    {
        private readonly List<ICommandHandler<ICommand>> commandHandlers;
        public CommandBus(IEnumerable<ICommandHandler<ICommand>> commandHandlers)
        {
            this.commandHandlers = commandHandlers.ToList();
        }

        public void Dispatch(ICommand command)
        {
            var handlers = commandHandlers.OfType<ICommandHandler<ICommand>>().ToList();
            handlers.ForEach(x => x.HandleAsync(command));
        }
    }

}
