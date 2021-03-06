using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using Akka.Routing;
using Autofac;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.Aggregates.Messages;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.Actors.CommandPipe.MessageProcessors;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.Hadlers;
using GridDomain.Node.Actors.ProcessManagers;
using GridDomain.Node.Configuration;
using GridDomain.Node.Configuration.Composition;
using GridDomain.ProcessManagers.DomainBind;
using GridDomain.ProcessManagers.State;

namespace GridDomain.Node
{
    public class LocalCommandPipe: IActorCommandPipe
    {
        private readonly IDictionary<string, IActorRef> _aggregatesCatalog = new Dictionary<string, IActorRef>();
        private readonly ICompositeMessageProcessor _handlersCatalog;

        private readonly ILoggingAdapter _log;
        private readonly ICompositeMessageProcessor<ProcessesTransitComplete, IProcessCompleted> _processCatalog;
        public ActorSystem System { get; }

        public LocalCommandPipe(ActorSystem system,
                                ICompositeMessageProcessor handlersProcessor = null,
                                ICompositeMessageProcessor<ProcessesTransitComplete, IProcessCompleted> processProcessor = null)
        {
            System = system;
            _log = system.Log;
            _handlersCatalog = handlersProcessor ?? new HandlersDefaultProcessor();
            _processCatalog = processProcessor ?? new ProcessesDefaultProcessor();
            
            ProcessesPipeActor = System.ActorOf(Props.Create(() => new LocalProcessesPipeActor(_processCatalog)), nameof(LocalProcessesPipeActor));

            HandlersPipeActor = System.ActorOf(Props.Create(() => new HandlersPipeActor(_handlersCatalog, "/user/"+nameof(LocalProcessesPipeActor))),
                                               nameof(Actors.CommandPipe.HandlersPipeActor));

            CommandExecutor = System.ActorOf(Props.Create(() => new AggregatesPipeActor(_aggregatesCatalog)),
                                             nameof(AggregatesPipeActor));
        }

        public IActorRef ProcessesPipeActor { get; private set; }
        public IActorRef HandlersPipeActor { get; private set; }
        public IActorRef CommandExecutor { get; internal set; }
        public IContainerConfiguration Prepare()
        {
            return new ContainerConfiguration(c =>
                                              {
                                                  c.RegisterInstance(HandlersPipeActor)
                                                                        .Named<IActorRef>(Actors.CommandPipe.HandlersPipeActor.CustomHandlersProcessActorRegistrationName);
                                                  c.RegisterInstance(ProcessesPipeActor)
                                                                        .Named<IActorRef>(Actors.CommandPipe.ProcessesPipeActor.ProcessManagersPipeActorRegistrationName);
                                              });
        }

        private readonly List<Action> _actorProducers = new List<Action>();
        
        public Task RegisterAggregate(IAggregateCommandsHandlerDescriptor descriptor)
        {
            _actorProducers.Add(() =>
                                        {
                                            StartAggregate(descriptor);
                                        });
            return Task.CompletedTask;
        }

        private void StartAggregate(IAggregateCommandsHandlerDescriptor descriptor)
        {
            var aggregateHubType = typeof(AggregateHubActor<>).MakeGenericType(descriptor.AggregateType);
            var aggregateActor = CreateDIActor(aggregateHubType, descriptor.AggregateType.BeautyName() + "_Hub");

                _aggregatesCatalog.Add(descriptor.AggregateType.Name, aggregateActor);
        }

        public Task RegisterProcess(IProcessDescriptor processDescriptor, string name = null)
        {
            _actorProducers.Add(() =>
                                        {
                                            StartProcess(processDescriptor,name);
                                        });
            return Task.CompletedTask;
        }

        private void StartProcess(IProcessDescriptor processDescriptor, string name)
        {
            var processHubActorType = typeof(ProcessHubActor<>).MakeGenericType(processDescriptor.StateType);
            var processHubActor = CreateDIActor(processHubActorType, name ?? processDescriptor.ProcessType.BeautyName() + "_Hub");

            var processStateHubType = typeof(ProcessStateHubActor<>).MakeGenericType(processDescriptor.StateType);
            //will be consumed in ProcessActor
            var processStateHubActor = CreateDIActor(processStateHubType, processDescriptor.StateType.BeautyName() + "_Hub");

            var processor = new ActorAskMessageProcessor<IProcessCompleted>(processHubActor);


            foreach (var acceptMsg in processDescriptor.AcceptMessages)
                _processCatalog.Add(acceptMsg.MessageType, processor);
        }

        public Task RegisterSyncHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                              where TMessage : class, IHaveProcessId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new ActorAskMessageProcessor<HandlerExecuted>(actor));
        }

        public Task RegisterFireAndForgetHandler<TMessage, THandler>() where THandler : IHandler<TMessage> where TMessage : class, IHaveProcessId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new FireAndForgetActorMessageProcessor(actor));
        }

        public Task Start()
        {
            foreach (var postponed in _actorProducers)
                postponed();

            return ProcessesPipeActor.Ask<Initialized>(new Initialize(CommandExecutor));
        }

        private Task RegisterHandler<TMessage, THandler>(Func<IActorRef, IMessageProcessor> processorCreator) where THandler : IHandler<TMessage>
                                                                                                              where TMessage : class, IHaveProcessId, IHaveId
        {
            _actorProducers.Add(() =>
                                        {
                                            var handlerActorType = typeof(MessageHandleActor<TMessage, THandler>);
                                            var handlerActor = CreateDIActor(handlerActorType, handlerActorType.BeautyName());

                                            _handlersCatalog.Add<TMessage>(processorCreator(handlerActor));
                                        });
            
            return Task.CompletedTask;
        }

        private IActorRef CreateDIActor(Type actorType, string actorName, RouterConfig routeConfig = null)
        {
            var diActorSystemAdapter = System.DI();
            
            var actorProps = diActorSystemAdapter.Props(actorType);
            if (routeConfig != null)
                actorProps = actorProps.WithRouter(routeConfig);

            var actorRef = System.ActorOf(actorProps, actorName);
            return actorRef;
        }

        public void Dispose()
        {
            System.Dispose();
        }
    }
}