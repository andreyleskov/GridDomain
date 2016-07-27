using System;
using System.Collections.Generic;
using System.Threading;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.VersionedTypeSerialization;
using GridDomain.Node;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.EventsUpgrade.Domain;
using GridDomain.Tests.EventsUpgrade.Domain.Events;
using GridDomain.Tests.Framework.Persistence;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace GridDomain.Tests.Acceptance.EventsUpgrade.SampleDomain
{

    class BalanceChangedEventAdapter1 : DomainEventAdapter<BalanceChangedEvent_V0, BalanceChangedEvent_V1>
    {
        public override IEnumerable<BalanceChangedEvent_V1> ConvertEvent(BalanceChangedEvent_V0 evt)
        {
            yield return new BalanceChangedEvent_V1(evt.AmplifiedAmountChange, evt.SourceId);
        }
    }

    [TestFixture]
    public class Given_aggregate_with_upgraded_event_with_new_field: PersistentTest
    {
        private Guid _balanceId;
        private BalanceAggregate _aggregate;

        [TestFixtureSetUp]
        public void When_aggregate_is_recovered_from_persistence()
        {
            _balanceId = Guid.NewGuid();
            var events = new DomainEvent[]
            {
                new AggregateCreatedEvent(0, _balanceId),
                new BalanceChangedEvent_V0(5, _balanceId,2),
                new BalanceChangedEvent_V1(5, _balanceId),
            };

            SaveInJournal<BalanceAggregate>(_balanceId, events);
            Thread.Sleep(500);

            var domainEventsUpgradeChain = new DomainEventsUpgradeChain();
            domainEventsUpgradeChain.Register(new BalanceChangedEventAdapter1());
            AkkaDomainEventsAdapter.UpgradeChain = domainEventsUpgradeChain;

            _aggregate = LoadAggregate<BalanceAggregate>(_balanceId);
        }

        protected override TimeSpan Timeout => TimeSpan.FromSeconds(1);

        [Test]
        public void Then_it_should_process_old_and_new_event()
        {
            Assert.AreEqual(15, _aggregate.Amount);
        }

        public Given_aggregate_with_upgraded_event_with_new_field(bool inMemory) : base(inMemory)
        {
        }
        public Given_aggregate_with_upgraded_event_with_new_field() : base(true)
        {
        }

        protected override IContainerConfiguration CreateConfiguration()
        {
            return new CustomContainerConfiguration(c => c.RegisterAggregate<BalanceAggregate,BalanceAggregatesCommandHandler>(),
                                                    c => c.RegisterInstance<IQuartzConfig>(new InMemoryQuartzConfig()));
        }

        protected override IMessageRouteMap CreateMap()
        {
            return new BalanceRouteMap();
        }
        
    }
}