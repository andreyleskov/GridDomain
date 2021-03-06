using GridDomain.Tests.Common.Configuration;
using GridDomain.Tests.Unit.CommandsExecution;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.Cluster.CommandsExecution
{
    public class Cluster_AsyncExecute_without_timeout : AsyncExecute_without_timeout
    {
        public Cluster_AsyncExecute_without_timeout(ITestOutputHelper output)
            : base(new NodeTestFixture(output,
                                       nameof(Cluster_AsyncExecute_without_timeout))
                   .Clustered()
                   .LogToFile(nameof(Cluster_AsyncExecute_without_timeout))) { }
    }
}