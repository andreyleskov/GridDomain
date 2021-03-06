using System;
using System.Collections.Generic;
using Automatonymous;
using GridDomain.CQRS;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.DomainBind;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Commands;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using Serilog;

namespace GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain
{
    public class SoftwareProgrammingProcess : ConventionProcess<SoftwareProgrammingState>
    {
        public SoftwareProgrammingProcess()
        {
            CompositeEvent(() => Composite,x => x.CompositeTrack, GotTired, CoffeNotAvailable);

            During(Coding,
                   When(GotTired).Then(context =>
                                       {
                                           var state = context.Instance;
                                           var domainEvent = context.Data;
                                           state.PersonId = domainEvent.SourceId;
                                           Dispatch(new MakeCoffeCommand(domainEvent.SourceId, state.CoffeeMachineId));
                                       })
                                 .TransitionTo(MakingCoffee),
                   When(SleptWell).Then(ctx => ctx.Instance.SofaId = ctx.Data.SofaId).TransitionTo(Coding),
                   When(Composite).Then(ctx => { }));

            During(MakingCoffee,
                   When(CoffeNotAvailable).Then(context =>
                                                {
                                                    var coffeMachineId = context.Data.CoffeMachineId;
                                                    //Just for testing purposes
                                                    if (string.IsNullOrEmpty(coffeMachineId) || coffeMachineId.Length < 2)
                                                        throw new UndefinedCoffeMachineException();

                                                    Dispatch(new GoSleepCommand(context.Data.ForPersonId,
                                                                                context.Instance.SofaId));
                                                })
                                          .TransitionTo(Sleeping),
                   When(CoffeReady).TransitionTo(Coding),
                   When(Composite).Then(ctx => { }));

            During(Sleeping,
                   When(SleptBad).Then(ctx => ctx.Instance.BadSleepPersonId = ctx.Data.Message.PersonId).TransitionTo(Coding),
                   When(SleptWell).Then(ctx => ctx.Instance.SofaId = ctx.Data.SofaId).TransitionTo(Coding));
        }

        public Event<GotTiredEvent> GotTired { get; private set; }
        public Event<CoffeMadeEvent> CoffeReady { get; private set; }
        public Event<SleptWellEvent> SleptWell { get; private set; }
        public Event<Fault<GoSleepCommand>> SleptBad { get; private set; }
        public Event<CoffeMakeFailedEvent> CoffeNotAvailable { get; private set; }
        public Event Composite { get; private set; }

        public State Coding { get; private set; }
        public State MakingCoffee { get; private set; }
        public State Sleeping { get; private set; }
    }
}