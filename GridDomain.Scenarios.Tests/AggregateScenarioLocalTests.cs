﻿using System.Threading.Tasks;
using GridDomain.Scenarios.Runners;
using Xunit.Abstractions;

namespace GridDomain.Scenarios.Tests {
    public class AggregateScenarioLocalTests : AggregateScenarioTests
    {
        protected override Task<IAggregateScenarioRun<T>> Run<T>(IAggregateScenario<T> scenario)
        {
            return scenario.Run()
                           .Local(Logger);
        }

        public AggregateScenarioLocalTests(ITestOutputHelper output) : base(output) { }
    }
}