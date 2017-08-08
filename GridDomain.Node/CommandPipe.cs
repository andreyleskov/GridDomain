using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using Akka.Routing;
using GridDomain.Common;
using GridDomain.Configuration.MessageRouting;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.Actors.CommandPipe.MessageProcessors;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.Hadlers;
using GridDomain.Node.Actors.ProcessManagers;
using GridDomain.ProcessManagers.DomainBind;
using Microsoft.Practices.Unity;

namespace GridDomain.Node
{
    public class CommandPipe : IMessagesRouter
    {
        private readonly TypeCatalog<IMessageProcessor, ICommand> _aggregatesCatalog = new TypeCatalog<IMessageProcessor, ICommand>();
        private readonly IUnityContainer _container;
        private readonly ProcessorListCatalog _handlersCatalog = new ProcessorListCatalog();

        private readonly ILoggingAdapter _log;
        private readonly ProcessorListCatalog<IProcessCompleted> _processCatalog = new ProcessorListCatalog<IProcessCompleted>();
        private readonly ActorSystem _system;

        public CommandPipe(ActorSystem system, IUnityContainer container)
        {
            _container = container;
            _system = system;
            _log = system.Log;
        }

        public IActorRef ProcessesPipeActor { get; private set; }
        public IActorRef HandlersPipeActor { get; private set; }
        public IActorRef CommandExecutor { get; private set; }

        public Task RegisterAggregate(IAggregateCommandsHandlerDescriptor descriptor)
        {
            var aggregateHubType = typeof(AggregateHubActor<>).MakeGenericType(descriptor.AggregateType);

            var aggregateActor = CreateActor(aggregateHubType, descriptor.AggregateType.BeautyName() + "_Hub");

            var processor = new FireAndForgetMessageProcessor(aggregateActor);

            foreach (var aggregateCommandInfo in descriptor.RegisteredCommands)
                _aggregatesCatalog.Add(aggregateCommandInfo, processor);

            return Task.CompletedTask;
        }

        public Task RegisterProcess(IProcessManagerDescriptor processManagerDescriptor, string name = null)
        {
            var processActorType = typeof(ProcessManagerHubActor<>).MakeGenericType(processManagerDescriptor.StateType);

            var processManagerActor = CreateActor(processActorType, name ?? processManagerDescriptor.ProcessType.BeautyName() + "_Hub");
            var processor = new SynchronousMessageProcessor<IProcessCompleted>(processManagerActor);

            foreach (var acceptMsg in processManagerDescriptor.AcceptMessages)
                _processCatalog.Add(acceptMsg.MessageType, processor);

            return Task.CompletedTask;
        }

        public Task RegisterSyncHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                                                 where TMessage : class, IHaveProcessId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new SynchronousMessageProcessor<HandlerExecuted>(actor));
        }
        public Task RegisterFireAndForgetHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                              where TMessage : class, IHaveProcessId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new FireAndForgetMessageProcessor(actor));
        }
        public Task RegisterParralelHandler<TMessage, THandler>() where THandler : IHandler<TMessage>
                                                                       where TMessage : class, IHaveProcessId, IHaveId
        {
            return RegisterHandler<TMessage, THandler>(actor => new ParallelMessageProcessor<HandlerExecuted>(actor));
        }

        /// <summary>
        /// </summary>
        /// <returns>Reference to pipe actor for command execution</returns>
        public async Task<IActorRef> Init()
        {
            _log.Debug("Command pipe is starting");

            ProcessesPipeActor = _system.ActorOf(Props.Create(() => new ProcessManagersPipeActor(_processCatalog)), nameof(ProcessManagersPipeActor));

            HandlersPipeActor = _system.ActorOf(Props.Create(() => new HandlersPipeActor(_handlersCatalog, ProcessesPipeActor)),
                                                nameof(Actors.CommandPipe.HandlersPipeActor));

            CommandExecutor = _system.ActorOf(Props.Create(() => new AggregatesPipeActor(_aggregatesCatalog)),
                                              nameof(AggregatesPipeActor));

            _container.RegisterInstance(Actors.CommandPipe.HandlersPipeActor.CustomHandlersProcessActorRegistrationName, HandlersPipeActor);
            _container.RegisterInstance(ProcessManagersPipeActor.ProcessManagersPipeActorRegistrationName, ProcessesPipeActor);

            await ProcessesPipeActor.Ask<Initialized>(new Initialize(CommandExecutor));
            return CommandExecutor;
        }

        private Task RegisterHandler<TMessage, THandler>(Func<IActorRef, IMessageProcessor> processorCreator) where THandler : IHandler<TMessage>
                                                                           where TMessage : class, IHaveProcessId, IHaveId
        {
            var handlerActorType = typeof(MessageProcessActor<TMessage, THandler>);
            var handlerActor = CreateActor(handlerActorType, handlerActorType.BeautyName());

            _handlersCatalog.Add<TMessage>(processorCreator(handlerActor));
            return Task.CompletedTask;
        }

        private IActorRef CreateActor(Type actorType, string actorName, RouterConfig routeConfig = null)
        {
            var actorProps = _system.DI().Props(actorType);
            if (routeConfig != null)
                actorProps = actorProps.WithRouter(routeConfig);

            var actorRef = _system.ActorOf(actorProps, actorName);
            return actorRef;
        }
    }
}