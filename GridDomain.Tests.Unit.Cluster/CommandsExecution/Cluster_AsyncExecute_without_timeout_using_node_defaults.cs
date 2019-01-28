using System;
using GridDomain.Tests.Common.Configuration;
using GridDomain.Tests.Unit.CommandsExecution;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.Cluster.CommandsExecution
{
    public class Cluster_AsyncExecute_without_timeout_using_node_defaults : AsyncExecute_without_timeout_using_node_defaults
    {
        public Cluster_AsyncExecute_without_timeout_using_node_defaults(ITestOutputHelper output)
            : base(
                   new NodeTestFixture(output,
                                       cfg: new AutoTestNodeConfiguration(name: "Cluster-asyncExecute-without-timeout-using-node-defaults"),
                                       defaultTimeout: TimeSpan.FromMilliseconds(1)).Clustered()) { }
    }
}