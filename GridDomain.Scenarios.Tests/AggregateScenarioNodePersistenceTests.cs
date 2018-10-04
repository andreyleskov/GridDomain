using System.Threading.Tasks;
using GridDomain.Configuration;
using GridDomain.Scenarios.Runners;
using GridDomain.Tests.Acceptance;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using Xunit.Abstractions;

namespace GridDomain.Scenarios.Tests {
    public class AggregateScenarioNodePersistenceTests : AggregateScenarioTests
    {
        protected override async Task<IAggregateScenarioRun<T>> Run<T>(IAggregateScenario<T> scenario)
        {
            var cfg = new AutoTestNodeDbConfiguration();
            await TestDbTools.Delete(cfg.JournalConnectionString, "Journal");
            
            return await scenario.Run()
                                 .Node(new DomainConfiguration(new BalloonDomainConfiguration(),
                                                               new ProgrammerAggregateDomainConfiguration()),
                                       Logger,
                                       cfg.JournalConnectionString);
        }

        public AggregateScenarioNodePersistenceTests(ITestOutputHelper output) : base(output) { }
    }
}