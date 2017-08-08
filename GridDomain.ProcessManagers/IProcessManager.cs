using System.Threading.Tasks;

namespace GridDomain.ProcessManagers
{
    public interface IProcessManager<TState> where TState : IProcessState
    {
        TState State { get; set; }
        Task<ProcessResult<TState>> Transit<T>(T message) where T : class;
    }
}