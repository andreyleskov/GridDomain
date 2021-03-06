﻿using System;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Scheduling.Quartz.Configuration;
using GridDomain.Tests.Acceptance.Snapshots;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.EventsUpgrade;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Commands;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.EventsUpgrade
{
    public class Future_events_upgraded_by_object_adapter : NodeTestKit
    {
        public Future_events_upgraded_by_object_adapter(ITestOutputHelper output)
            : this(ConfigureDomain(new BalanceFixture(output,new PersistedQuartzConfig()))) { }

        protected Future_events_upgraded_by_object_adapter(NodeTestFixture fixture) : base(fixture) { }

        protected static NodeTestFixture ConfigureDomain(BalanceFixture balanceFixture)
        {
            var cfg = new PersistedQuartzConfig();
            return balanceFixture.UseSqlPersistence()
                                 .InitFastRecycle(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(400))
                                 .UseAdaper(new IncreaseBy100Adapter())
                                 .ClearQuartzPersistence(cfg.ConnectionString);
        }

        private class IncreaseBy100Adapter : ObjectAdapter<BalanceChangedEvent_V1, BalanceChangedEvent_V1>
        {
            public override BalanceChangedEvent_V1 Convert(BalanceChangedEvent_V1 evt)
            {
                return new BalanceChangedEvent_V1(evt.AmountChange + 100, evt.SourceId, evt.CreatedTime, evt.ProcessId);
            }
        }

        [Fact]
        public async Task Future_event_is_upgraded_by_json_adapter()
        {
            var cmd = new ChangeBalanceInFuture(1,
                                                Guid.NewGuid()
                                                    .ToString(),
                                                BusinessDateTime.Now.AddSeconds(2),
                                                false);

            var res = await Node.Prepare(cmd)
                                .Expect<BalanceChangedEvent_V1>()
                                .Execute(TimeSpan.FromSeconds(10));

            //event should be modified by json object adapter, changing its Amount
            Assert.Equal(101,
                         res.Message<BalanceChangedEvent_V1>()
                            .AmountChange);
        }
    }
}