using System;
using GridDomain.CQRS;
using GridDomain.ProcessManagers;

namespace GridDomain.Tests.Unit.FutureEvents.Infrastructure
{
    public class CancelFutureEventCommand : Command<TestFutureEventsAggregate>
    {
        public CancelFutureEventCommand(string aggregateId, string value) : base(aggregateId)
        {
            Value = value;
        }

        public string Value { get; }
    }
}