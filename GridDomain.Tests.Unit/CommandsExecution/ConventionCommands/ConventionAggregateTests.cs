﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Commands;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution.ConventionCommands
{

    public class ConventionAggregateTests:NodeTestKit
    {
        public ConventionAggregateTests(ITestOutputHelper output) : this(new NodeTestFixture(output,new SoftwareDomainConfiguration())) { }
        protected ConventionAggregateTests(NodeTestFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ConventionRegisteredCommands_are_executed()
        {
            await Node.Prepare(new GoSleepCommand(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()))
                      .Expect<Slept>()
                      .Execute();
        }
        [Fact]
        public async Task ConventionRegistered_aggregate_can_be_created()
        {
            await Node.Prepare(new CreatePersonCommand(Guid.NewGuid().ToString()))
                      .Expect<PersonCreated>()
                      .Execute();
        }
        [Fact]
        public void ConventionRegisteredEvents_are_applied()
        {
            var person = AggregateFactory.BuildEmpty<ProgrammerAggregate>();
            var personCreated = new PersonCreated(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            ((IAggregate)person).Apply(personCreated);

            Assert.Equal(personCreated.PersonId,person.PersonId);
        }
    }
}
