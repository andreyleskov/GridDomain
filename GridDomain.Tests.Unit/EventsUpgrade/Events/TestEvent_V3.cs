using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.EventsUpgrade.Events
{
    public class TestEvent_V3 : DomainEvent
    {
        public TestEvent_V3(Guid sourceId, DateTime? createdTime = null, Guid sagaId = new Guid())
            : base(sourceId, sagaId: sagaId, createdTime: createdTime) {}

        public TestEvent_V3() : this(Guid.Empty) {}

        public int Field4 { get; set; }
    }
}