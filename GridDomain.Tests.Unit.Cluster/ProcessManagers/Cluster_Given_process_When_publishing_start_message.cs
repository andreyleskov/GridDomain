using System.Threading.Tasks;
using GridDomain.Tests.Unit.ProcessManagers;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.Cluster.ProcessManagers
{
    public class Cluster_Given_process_When_publishing_start_message : Given_process_When_publishing_start_message
    {
        public Cluster_Given_process_When_publishing_start_message(ITestOutputHelper helper) :
            base(new SoftwareProgrammingProcessManagerFixture(helper).Clustered()) { }

        protected override async Task<SoftwareProgrammingState> GetProcessTransitedState(SleptWellEvent startMessage)
        {
            return await Node.GetTransitedState<SoftwareProgrammingState>(startMessage);
        }
    }
}