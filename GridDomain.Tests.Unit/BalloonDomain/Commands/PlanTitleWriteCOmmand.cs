using System;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.ProcessManagers;

namespace GridDomain.Tests.Unit.BalloonDomain.Commands
{
    public class PlanTitleWriteCommand : Command<Balloon> 
    {
       
        public PlanTitleWriteCommand(int parameter, string aggregateId) : base(aggregateId)
        {
            Parameter = parameter;
        }
         public PlanTitleWriteCommand(int parameter, Guid aggregateId) : this(parameter,aggregateId.ToString())
        {
        }

        public int Parameter { get; }
    }
}