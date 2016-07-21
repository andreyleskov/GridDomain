using System;
using System.Linq;
using CommonDomain;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas.FutureEvents;
using GridDomain.Node.FutureEvents;
using GridDomain.Tests.FutureEvents.Infrastructure;
using NUnit.Framework;

namespace GridDomain.Tests.FutureEvents
{


    public static class AggregateDebugExtensions
    {

        public static void ApplyEvents(this IAggregate aggregate, params DomainEvent[] events)
        {
            foreach(var e in events)
                aggregate.ApplyEvent(e);
        }

        public static void ClearEvents(this IAggregate aggregate)
        {
            aggregate.ClearUncommittedEvents();
        }

        public static TEvent[] GetEvents<TEvent>(this IAggregate aggregate) where TEvent:DomainEvent
        {
            return aggregate.GetUncommittedEvents().OfType<TEvent>().ToArray();
        }

        public static TEvent GetEvent<TEvent>(this IAggregate aggregate) where TEvent : DomainEvent
        {
            var @event = aggregate.GetUncommittedEvents().OfType<TEvent>().FirstOrDefault();
            if (@event == null)
                throw new CannotFindRequestedEventException();
            return @event;
        }
    }

    public class CannotFindRequestedEventException : Exception
    {
    }

    [TestFixture]

    public class Given_aggregate_When_raising_future_event_by_methods : FutureEventsTest_InMemory
    {
        private TestAggregate _aggregate;
        private DateTime _scheduledTime;
        private TestDomainEvent _producedEvent;
        private RaiseEventInFutureCommand _testCommand;
        private FutureDomainEvent _futureEventEnvelop;
        private FutureDomainEventOccuredEvent _futureDomainEventOccuredEvent;

        [TestFixtureSetUp]

        public void When_raising_future_event()
        {
            _testCommand = new RaiseEventInFutureCommand(_scheduledTime, Guid.NewGuid(), "test value");

            _aggregate = new TestAggregate(_testCommand.AggregateId);
            _aggregate.ScheduleInFuture(_testCommand.RaiseTime, _testCommand.Value);

            _futureEventEnvelop = _aggregate.GetEvent<FutureDomainEvent>();
            _aggregate.RaiseScheduledEvent(_futureEventEnvelop.Id);
            _producedEvent = _aggregate.GetEvent<TestDomainEvent>();
            _futureDomainEventOccuredEvent = _aggregate.GetEvent<FutureDomainEventOccuredEvent>();
        }

        [Then]
        public void Future_event_occurance_has_same_id_as_future_event()
        {
            Assert.AreEqual(_futureEventEnvelop.Id, _futureDomainEventOccuredEvent.FutureEventId);
        }

        [Then]
        public void Future_event_applies_to_aggregate()
        {
            Assert.AreEqual(_producedEvent.Value, _aggregate.Value);
        }

        [Then]
        public void Future_event_envelop_has_id_different_from_aggregate()
        {
            Assert.AreNotEqual(_futureEventEnvelop.Id, _aggregate.Value);
        }

        [Then]
        public void Future_event_sourceId_is_aggregate_id()
        {
            Assert.AreEqual(_futureEventEnvelop.SourceId, _aggregate.Id);
        }

        [Then]
        public void Future_event_payload_is_aggregate_original_event()
        {
            Assert.AreEqual(((TestDomainEvent)_futureEventEnvelop.Event).Id, _producedEvent.Id);
        }

        [Then]
        public void Future_event_contains_data_from_command()
        {
            Assert.AreEqual(_testCommand.Value, _producedEvent.Value);
        }
    }
}