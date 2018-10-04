using System.Threading.Tasks;
using GridDomain.Configuration;
using GridDomain.Scenarios.Runners;
using GridDomain.Tests.Acceptance;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using Xunit.Abstractions;

namespace GridDomain.Scenarios.Tests {
    public class AggregateScenarioClusterPersistenceTests : AggregateScenarioTests
    {
        private readonly ITestOutputHelper _output;

        protected override async Task<IAggregateScenarioRun<T>> Run<T>(IAggregateScenario<T> scenario)
        {
            
            var cfg = new AutoTestNodeDbConfiguration();
            await TestDbTools.Delete(cfg.JournalConnectionString, "Journal");

            return await scenario.Run()
                                 .Cluster(new DomainConfiguration(new BalloonDomainConfiguration(),
                                                                  new ProgrammerAggregateDomainConfiguration()),
                                          cfg.JournalConnectionString,
                                          () =>new XUnitAutoTestLoggerConfiguration(_output));
        }

        public AggregateScenarioClusterPersistenceTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }
    }
}