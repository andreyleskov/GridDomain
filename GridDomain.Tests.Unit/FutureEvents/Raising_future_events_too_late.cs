using System;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit.FutureEvents.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.FutureEvents
{
    public class Raising_future_events_too_late : NodeTestKit
    {
        public Raising_future_events_too_late(ITestOutputHelper output) : base(new FutureEventsFixture(output)) {}
        protected Raising_future_events_too_late(NodeTestFixture fixture) : base(fixture) {}

        [Fact]
        public async Task Given_aggregate_When_raising_future_event_in_past_Then_it_fires_immediatly()
        {
            var scheduledTime = DateTime.Now.AddSeconds(-5);
            var now = DateTime.Now;
            var testCommand = new ScheduleEventInFutureCommand(scheduledTime, Guid.NewGuid().ToString(), "test value");

            await Node.Prepare(testCommand)
                      .Expect<JobSucceeded>()
                      .Execute();

            var aggregate = await Node.LoadAggregate<TestFutureEventsAggregate>(testCommand.AggregateId);

            Assert.True(now.Second - aggregate.ProcessedTime.Second <= 1);
        }
    }
}