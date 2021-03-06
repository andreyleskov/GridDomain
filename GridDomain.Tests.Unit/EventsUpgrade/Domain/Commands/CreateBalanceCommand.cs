using System;
using GridDomain.CQRS;
using GridDomain.ProcessManagers;

namespace GridDomain.Tests.Unit.EventsUpgrade.Domain.Commands
{
    public class CreateBalanceCommand : Command<BalanceAggregate>
    {
        public CreateBalanceCommand(int parameter, string aggregateId) : base(aggregateId)
        {
            Parameter = parameter;
        }

        public int Parameter { get; }
    }
}