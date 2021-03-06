using System;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Scheduling.Quartz.Configuration;
using GridDomain.Tests.Acceptance.Snapshots;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.EventsUpgrade;
using GridDomain.Tests.Unit.EventsUpgrade.Domain;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Commands;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Events;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.EventsUpgrade
{
    public class Future_events_class_upgraded_by_object_adapter : NodeTestKit
    {
        public Future_events_class_upgraded_by_object_adapter(ITestOutputHelper output)
            : this(CreateFixture(output)) { }

        protected Future_events_class_upgraded_by_object_adapter(NodeTestFixture fixture) : base(fixture) { }

        protected static NodeTestFixture CreateFixture(ITestOutputHelper output)
        {
            var persistedQuartzConfig = new PersistedQuartzConfig();
            return new BalanceFixture(output, persistedQuartzConfig).InitFastRecycle()
                                                                    .UseSqlPersistence()
                                                                    .UseAdaper(new BalanceChanged_eventdapter1())
                                                                    .ClearQuartzPersistence(persistedQuartzConfig.ConnectionString)
                                                                    .LogLevel(LogEventLevel.Verbose)
                                                                    .PrintSystemConfig();
        }

        private class BalanceChanged_eventdapter1 : ObjectAdapter<BalanceChangedEvent_V0, BalanceChangedEvent_V1>
        {
            public override BalanceChangedEvent_V1 Convert(BalanceChangedEvent_V0 evt)
            {
                return new BalanceChangedEvent_V1(evt.AmplifiedAmountChange, evt.SourceId);
            }
        }

        [Fact]
        public async Task Future_event_is_upgraded_by_event_adapter()
        {
            await Node.Execute(new ChangeBalanceInFuture(1, "update_event_aggregate", BusinessDateTime.Now.AddSeconds(2), true));

            await Node.NewWaiter(TimeSpan.FromSeconds(10))
                      .Expect<BalanceChangedEvent_V1>()
                      .Create();
        }
    }
}