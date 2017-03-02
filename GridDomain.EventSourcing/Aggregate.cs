﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonDomain;
using CommonDomain.Core;
using GridDomain.EventSourcing.FutureEvents;

namespace GridDomain.EventSourcing
{

    public class Aggregate : AggregateBase,
                             IMemento
    {
        private static readonly AggregateFactory Factory = new AggregateFactory();

        private static readonly Action<DomainEvent> EmptyApply = e => { };
        private static readonly Action EmptyContinue = () => { };

        //internal Action<DomainEvent[], Action<DomainEvent>> PersistDelegate;
        internal Action<Task<DomainEvent[]>, Action<DomainEvent>, Action, Aggregate> PersistAsyncDelegate;

        private readonly HashSet<DomainEvent> _eventToPersist = new HashSet<DomainEvent>();
        public IReadOnlyCollection<DomainEvent> EventToPersist => _eventToPersist;
        public void MarkPersisted(DomainEvent e)
        {
            _eventToPersist.Remove(e);
        }
        // Only for simple implementation 
        Guid IMemento.Id
        {
            get { return Id; }
            set { Id = value; }
        }

        int IMemento.Version
        {
            get { return Version; }
            set { Version = value; }
        }

        protected void Emit(DomainEvent e, Action<DomainEvent> onApply = null)
        {
            Emit(onApply, EmptyContinue, e);
        }
        protected void Emit(params DomainEvent[] e)
        {
            Emit(EmptyApply, EmptyContinue, e);

        }
        protected void Emit(Action afterAll, params DomainEvent[] e)
        {
            Emit(EmptyApply, afterAll, e);

        }

        protected void Emit(Action<DomainEvent> onApply, Action afterAll, params DomainEvent[] events)
        {
            foreach (var e in events)
                _eventToPersist.Add(e);

            PersistAsyncDelegate(Task.FromResult(events), onApply, afterAll, this);
        }

        protected void Emit<T>(Task<T> evtTask, Action<DomainEvent> onApply = null) where T : DomainEvent
        {
            Emit(evtTask.ContinueWith(t => new DomainEvent[] {t.Result}), onApply, EmptyContinue);
        }
        
        protected void Emit(Task<DomainEvent[]> evtTask, Action<DomainEvent> onApply = null, Action continuation = null)
        {
            var task = evtTask.ContinueWith(t =>
                                            {
                                                foreach (var e in t.Result)
                                                    _eventToPersist.Add(e);
                                                return t.Result;
                                            });

            PersistAsyncDelegate(task, onApply ?? EmptyApply, continuation ?? EmptyContinue, this);
        }
        protected void Emit(Task<DomainEvent[]> evtTask, Action continuation = null, Action<DomainEvent> onApply = null)
        {
            Emit(evtTask,onApply,continuation);
        }

      // public void RegisterPersistenceCallBack(Action<DomainEvent[], Action<DomainEvent>> persistDelegate)
      // {
      //   //  PersistDelegate = persistDelegate;
      // }
        public void RegisterPersistenceAsyncCallBack(Action<Task<DomainEvent[]>, Action<DomainEvent>, Action, Aggregate> persistDelegate)
        {
            PersistAsyncDelegate = persistDelegate;
        }

        public static T Empty<T>(Guid id) where T : IAggregate
        {
            return Factory.Build<T>(id);
        }

        protected void Apply<T>(Action<T> action) where T : DomainEvent
        {
            Register(action);
        }

        protected override IMemento GetSnapshot()
        {
            return this;
        }

        #region AsyncMethods

        public IDictionary<Guid, FutureEventScheduledEvent> FutureEvents { get; } =
            new Dictionary<Guid, FutureEventScheduledEvent>();

        #endregion

        #region FutureEvents

        protected Aggregate(Guid id)
        {
            Id = id;
            Register<FutureEventScheduledEvent>(Apply);
            Register<FutureEventOccuredEvent>(Apply);
            Register<FutureEventCanceledEvent>(Apply);
        }

        public void RaiseScheduledEvent(Guid futureEventId, Guid futureEventOccuredEventId)
        {
            FutureEventScheduledEvent e;
            if (!FutureEvents.TryGetValue(futureEventId, out e))
                throw new ScheduledEventNotFoundException(futureEventId);

            Emit(e.Event);
            Emit(new FutureEventOccuredEvent(futureEventOccuredEventId, futureEventId, Id));
        }

        protected void Emit(DomainEvent @event, DateTime raiseTime, Guid? futureEventId = null)
        {
            Emit(new FutureEventScheduledEvent(futureEventId ?? Guid.NewGuid(), Id, raiseTime, @event));
        }

        protected void CancelScheduledEvents<TEvent>(Predicate<TEvent> criteia = null) where TEvent : DomainEvent
        {
            var eventsToCancel = FutureEvents.Values.Where(fe => fe.Event is TEvent);
            if (criteia != null)
                eventsToCancel = eventsToCancel.Where(e => criteia((TEvent) e.Event));

            var domainEvents = eventsToCancel.Select(e => new FutureEventCanceledEvent(e.Id, Id))
                                             .Cast<DomainEvent>()
                                             .ToArray();
            Emit(domainEvents);
        }

        private void Apply(FutureEventScheduledEvent e)
        {
            FutureEvents[e.Id] = e;
        }

        private void Apply(FutureEventOccuredEvent e)
        {
            DeleteFutureEvent(e.FutureEventId);
        }

        private void Apply(FutureEventCanceledEvent e)
        {
            DeleteFutureEvent(e.FutureEventId);
        }

        private void DeleteFutureEvent(Guid futureEventId)
        {
            FutureEventScheduledEvent evt;
            if (!FutureEvents.TryGetValue(futureEventId, out evt))
                return;
            FutureEvents.Remove(futureEventId);
        }

        #endregion
    }
}