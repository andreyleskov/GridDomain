using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors.CommandPipe.ProcessorCatalogs;

namespace GridDomain.Node.Actors.CommandPipe
{
    /// <summary>
    /// Synhronize message handlers work for domain events produced by command
    /// If message process policy is set to synchronized, will process such events one after one
    /// Will process all other messages in parallel
    /// </summary>
    public class HandlersProcessActor : ReceiveActor
    {
        public const string CustomHandlersProcessActorRegistrationName = "CustomHandlersProcessActor";

        private readonly ICustomHandlersProcessorCatalog _handlersCatalog;
        private readonly IActorRef _sagasProcessActor;

        public HandlersProcessActor(ICustomHandlersProcessorCatalog handlersCatalog, IActorRef sagasProcessActor)
        {
            _sagasProcessActor = sagasProcessActor;
            _handlersCatalog = handlersCatalog;
    
            Receive<IMessageMetadataEnvelop<DomainEvent[]>>(envelop =>
            {
                var eventsToProcess = envelop.Message;
                var sender = Sender;
                eventsToProcess.Select(e => SynhronizeHandlers(new MessageMetadataEnvelop<DomainEvent>(e, envelop.Metadata)))
                               .ToChain()
                               .ContinueWith(t => new HandlersExecuted(envelop.Metadata, envelop.Message))
                               .PipeTo(Self, sender);
            });

            Receive<IMessageMetadataEnvelop<IFault>>(envelop =>
            {
                SynhronizeHandlers(envelop).ContinueWith(t => new HandlersExecuted(envelop.Metadata, envelop.Message))
                                           .PipeTo(Self, Sender);;
            });

            Receive<HandlersExecuted>(m =>
            {
                Sender.Tell(m); //notifying aggregate actor

                if(m.DomainEvents?.Length > 0)
                    _sagasProcessActor.Tell(new MessageMetadataEnvelop<DomainEvent[]>(m.DomainEvents,m.Metadata));

                if(m.Fault != null)
                    _sagasProcessActor.Tell(new MessageMetadataEnvelop<IFault>(m.Fault, m.Metadata));
            });
        }

        private Task SynhronizeHandlers(IMessageMetadataEnvelop messageMetadataEnvelop)
        {
            IReadOnlyCollection<Processor> faultProcessors = _handlersCatalog.GetHandlerProcessor(messageMetadataEnvelop.Message);
            if (!faultProcessors.Any()) return Task.CompletedTask;

            return faultProcessors.Select(p => {
                                             if (p.Policy.IsSynchronious)
                                                  return p.ActorRef.Ask<HandlerExecuted>(messageMetadataEnvelop);

                                              p.ActorRef.Tell(messageMetadataEnvelop);
                                              return Task.CompletedTask;
                                        })
                                  .ToChain();
        }
    }
}