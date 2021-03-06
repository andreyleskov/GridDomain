using System.Threading.Tasks;
using GridDomain.Common;

namespace GridDomain.CQRS
{
    public interface ICommandExecutor
    {
        Task Execute<T>(T command, IMessageMetadata metadata = null,CommandConfirmationMode confirm = CommandConfirmationMode.Projected)
            where T:ICommand;
        ICommandExpectationBuilder Prepare<U>(U cmd, IMessageMetadata metadata = null) where U : ICommand;
    }
    
    public interface ICommandHandler<in TCommand, TResult>
    {
        Task<TResult> Execute(TCommand command);
    }

    public interface ICommandHandler<in TCommand>
    {
        Task Execute(TCommand command);
    }

}