using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.CommandsExecution.ExecutionWithErrors;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution {
    public class Async_emit_after_await_results_to_the_new_state : NodeTestKit
    {
        public Async_emit_after_await_results_to_the_new_state(ITestOutputHelper output) : this(new NodeTestFixture(output)) { }
        protected Async_emit_after_await_results_to_the_new_state(NodeTestFixture fixure) : base(fixure.Add(new BalloonDomainConfiguration())) { }

        [Fact]
        public async Task When_aggregate_await_async_emit_produced_events_applied_to_state()
        {
            var asyncCommand = new DoubleIncreaseTitleCommand(43, "myBalloon");
            var received = new HashSet<string>();
            var waiter = Node.NewWaiter(TimeSpan.FromSeconds(1000)).Expect<BalloonTitleChanged>(e =>
                                                                      {
                                                                          received.Add(e.Id);
                                                                          return received.Count == 2;
                                                                      }).Create();
            await Node.Execute(asyncCommand);

            var res = waiter.Result;

            var event_generated_from_changed_state = res.Message<BalloonTitleChanged>();

            var finalValue = (asyncCommand.Value + 1).ToString();

            Assert.Equal(finalValue, event_generated_from_changed_state.Value);

            var sampleAggregate = await Node.LoadAggregate<Balloon>(asyncCommand.AggregateId);
            Assert.Equal(finalValue, sampleAggregate.Title);
        }
    }
}