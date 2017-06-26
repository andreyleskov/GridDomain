using System;
using GridDomain.CQRS;

namespace GridDomain.Tests.Unit.BalloonDomain
{
    public class IncreaseTitleCommand : Command
    {
        public IncreaseTitleCommand(int value, Guid aggregateId) : base(aggregateId)
        {
            Value = value;
        }

        public int Value { get; }
    }
}