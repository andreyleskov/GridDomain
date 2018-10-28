using System.Collections.Generic;
using GridDomain.Configuration;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GridDomain.Scenarios 
{
    public interface IAggregateScenario
    {
        IReadOnlyCollection<DomainEvent> ExpectedEvents { get; }
        IReadOnlyCollection<DomainEvent> GivenEvents { get; }
        IReadOnlyCollection<ICommand> GivenCommands { get; }
        string AggregateId { get; }
        string Name { get;}
    }


    public interface IAggregateScenario<T> : IAggregateScenario where T:IAggregate
    {
        IAggregateDependencies<T> Dependencies { get; }
    }
}