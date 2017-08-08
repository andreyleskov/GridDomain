using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.EventsUpgrade.Events
{
    public class TestEvent : DomainEvent
    {
        public TestEvent(Guid sourceId, DateTime? createdTime = null, Guid processId = new Guid())
            : base(sourceId, processId: processId, createdTime: createdTime) {}

        public TestEvent() : this(Guid.Empty) {}

        public int Field { get; set; }
    }
}