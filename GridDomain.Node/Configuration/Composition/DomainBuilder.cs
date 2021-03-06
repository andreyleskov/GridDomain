using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Util.Internal;
using Autofac;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.Configuration.MessageRouting;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.Hadlers;
using GridDomain.Node.Actors.ProcessManagers;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.State;

namespace GridDomain.Node.Configuration.Composition
{
    public class DomainBuilder : IDomainBuilder
    {
        protected readonly List<IMessageRouteMap> _maps = new List<IMessageRouteMap>();

        protected readonly List<IContainerConfiguration> _containerConfigurations = new List<IContainerConfiguration>();
        protected Func<Type, string> _processManagersStateActorPath;

        public IReadOnlyCollection<IContainerConfiguration> ContainerConfigurations => _containerConfigurations;

        public DomainBuilder(Func<Type,string> processManagersStateActorPath)
        {
            _processManagersStateActorPath = processManagersStateActorPath;
        }
        
        public void Configure(ContainerBuilder container)
        {
            ContainerConfigurations.ForEach(container.Register);

        }

        
        public async Task Configure(IMessagesRouter router)
        {
            foreach (var m in _maps)
                await m.Register(router);
        }

        public virtual void RegisterProcessManager<TState>(IProcessDependencyFactory<TState> processDependenciesfactory) where TState : class, IProcessState
        {
            _containerConfigurations.Add(new ProcessManagerConfiguration<TState,ProcessActor<TState>>(processDependenciesfactory,_processManagersStateActorPath(typeof(TState))));
            _maps.Add(processDependenciesfactory.CreateRouteMap());

            var stateConfig = new ProcessStateAggregateConfiguration<TState>(processDependenciesfactory.StateDependencies);
            _containerConfigurations.Add(stateConfig);
            _containerConfigurations.Add(new AggregateConfiguration<AggregateActor<ProcessStateAggregate<TState>>, ProcessStateAggregate<TState>>(processDependenciesfactory.StateDependencies));

            _maps.Add(processDependenciesfactory.StateDependencies.CreateRouteMap());
        }

        public void RegisterAggregate<TAggregate>(IAggregateDependencies<TAggregate> factory) where TAggregate : Aggregate
        {
            var aggregateConfiguration = CreateAggregateConfiguration(factory);
            _containerConfigurations.Add(aggregateConfiguration);
            _maps.Add(factory.CreateRouteMap());
        }

        protected virtual IContainerConfiguration CreateAggregateConfiguration<TAggregate>(IAggregateDependencies<TAggregate> factory) where TAggregate : Aggregate
        {
            return new AggregateConfiguration<AggregateActor<TAggregate>, TAggregate>(factory);
        }

        public void RegisterHandler<TContext,TMessage, THandler>(IMessageHandlerFactory<TContext,TMessage, THandler> factory) where THandler : IHandler<TMessage> where TMessage : class, IHaveProcessId, IHaveId
        {
            var cfg = new ContainerConfiguration(c => c.Register<THandler>(ctx => factory.Create(ctx.Resolve<TContext>())),
                                                 c => c.RegisterType<MessageHandleActor<TMessage, THandler>>());
            _containerConfigurations.Add(cfg);
            _maps.Add(factory.CreateRouteMap());
        }
    }
}