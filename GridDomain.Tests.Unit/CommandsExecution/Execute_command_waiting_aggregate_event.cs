using System;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.CommandsExecution.ExecutionWithErrors;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution
{
    public class Execute_command_waiting_aggregate_event : NodeTestKit
    {
        public Execute_command_waiting_aggregate_event(ITestOutputHelper output) : this(new NodeTestFixture(output)) {}
        protected Execute_command_waiting_aggregate_event(NodeTestFixture fixture) : base(fixture.Add(new BalloonDomainConfiguration())) {}

        [Fact]
        public async Task After_wait_ends_aggregate_should_be_changed()
        {
            var cmd = new PlanTitleWriteCommand(100, Guid.NewGuid());

            var res = await Node.Prepare(cmd)
                                .Expect<BalloonTitleChanged>(e => e.SourceId == cmd.AggregateId)
                                .Execute();

            var msg = res.Message<BalloonTitleChanged>();

            Assert.Equal(cmd.Parameter.ToString(), msg.Value);
        }

        [Fact]
        public async Task After_wait_of_async_command_aggregate_should_be_changed()
        {
            var cmd = new PlanTitleChangeCommand(Guid.NewGuid().ToString(), 42);
            var res = await Node.Prepare(cmd)
                                .Expect<BalloonTitleChanged>(e => e.SourceId == cmd.AggregateId)
                                .Execute();

            Assert.Equal(cmd.Parameter.ToString(), res.Message<BalloonTitleChanged>().Value);
        }

        [Fact]
        public async Task CommandWaiter_Should_wait_until_aggregate_event()
        {
            var cmd = new PlanTitleWriteCommand(100, Guid.NewGuid());

            var res = await Node.Prepare(cmd)
                                .Expect<BalloonTitleChanged>(e => e.SourceId == cmd.AggregateId)
                                .Execute();

            var msg = res.Message<BalloonTitleChanged>();

            Assert.Equal(cmd.Parameter.ToString(), msg.Value);
        }

        [Fact]
        public async Task CommandWaiter_will_wait_for_all_of_expected_message()
        {
            var cmd = new PlanTitleWriteCommand(1000, Guid.NewGuid());

            await Node.Prepare(cmd)
                      .Expect<BalloonTitleChanged>(e => e.SourceId == cmd.AggregateId)
                      .And<BalloonCreated>(e => e.SourceId == cmd.AggregateId)
                      .Execute()
                      .ShouldThrow<TimeoutException>();
        }
        
       

        [Fact]
        public async Task Wait_for_timeout_command_throws_excpetion()
        {
            var cmd = new PlanTitleWriteCommand(1000, Guid.NewGuid());
            await Node.Prepare(cmd)
                      .Expect<BalloonTitleChanged>(e => e.SourceId == cmd.AggregateId)
                      .Execute(TimeSpan.FromMilliseconds(100))
                      .ShouldThrow<TimeoutException>();
        }
    }
}