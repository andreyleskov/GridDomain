using Automatonymous;
using CommonDomain.Core;
using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using Microsoft.Practices.Unity;

namespace GridDomain.Node.Configuration.Composition
{
    public static class ContainerExtensions
    {
        public static void Register(this IUnityContainer container, IContainerConfiguration configuration)
        {
            configuration.Register(container);
        }

        public static void Register<TConfiguration>(this IUnityContainer container) where TConfiguration: IContainerConfiguration, new()
        {
            new TConfiguration().Register(container);
        }

        public static void RegisterAggregate<TAggregate, TCommandsHandler>(this IUnityContainer container) where TCommandsHandler : ICommandAggregateLocator<TAggregate>, IAggregateCommandsHandler<TAggregate> where TAggregate : AggregateBase
        {
            Register<AggregateConfiguration<TAggregate, TCommandsHandler>>(container);
        }

        public static void RegisterSaga<TSaga, TSagaData, TStartMessage,TFactory>(this IUnityContainer container) 
            where TSaga: Saga<TSagaData> 
            where TSagaData : class, ISagaState<State> 
            where TFactory : ISagaFactory<ISagaInstance, SagaDataAggregate<TSagaData>>,
                             ISagaFactory<ISagaInstance, TStartMessage>,
                             IEmptySagaFactory<ISagaInstance>
        {
            Register<InstanceSagaConfiguration<TSaga, TSagaData, TStartMessage,TFactory>>(container);
        }

    }
}