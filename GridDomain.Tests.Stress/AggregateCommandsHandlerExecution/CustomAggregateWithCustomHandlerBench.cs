﻿using GridDomain.EventSourcing;
using Xunit.Abstractions;

namespace GridDomain.Tests.Stress.AggregateCommandsHandlerExecution {
    public class CustomAggregateWithCustomHandlerBench : BallonCommandsExecutionBench<BenchmarkBalloonAggregate>
    {
        public CustomAggregateWithCustomHandlerBench(ITestOutputHelper output) : base(output) { }
    }
}