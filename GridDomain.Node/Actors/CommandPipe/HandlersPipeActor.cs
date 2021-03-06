using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Event;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors.CommandPipe.MessageProcessors;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.Hadlers;
using GridDomain.Node.Actors.Logging;
using GridDomain.Transport.Extension;

namespace GridDomain.Node.Actors.CommandPipe
{
    /// <summary>
    ///     Synhronize message handlers work for domain events produced by command
    ///     If message process policy is set to synchronized, will process such events one after one
    ///     Will process all other messages in parallel
    /// </summary>
    public class HandlersPipeActor : ReceiveActor, IWithUnboundedStash
    {
        private IMessageProcessor _handlersCatalog;
        public const string CustomHandlersProcessActorRegistrationName = "CustomHandlersProcessActor";
        protected ILoggingAdapter Log { get; } = Context.GetSeriLogger();


        public void InitializationBehavior(string processManagerPipeActorPath)
        {
            
            Context.ActorSelection(processManagerPipeActorPath)
                   .ResolveOne(TimeSpan.FromSeconds(5))
                   .PipeTo(Self);

            Receive<IActorRef>(r =>
                               {
                                   Stash.UnstashAll();
                                   Become(() => ProcessingMessagesBehavior(r));
                               });
            
            ReceiveAny(c => Stash.Stash());
        }
        public void ProcessingMessagesBehavior(IActorRef processManagerPipeActor)
        {
            Receive<Project>(envelop =>
                             {
                                 Log.Debug("Received messages to project. {project}", envelop);

                                 foreach (var e in envelop.Messages)
                                     _handlersCatalog.Process(e)
                                                    .ContinueWith(t =>
                                                                  {
                                                                      Log.Debug("Sent message {@message} to process managers",e);
                                                                      processManagerPipeActor.Tell(e);
                                                                      return AllHandlersCompleted.Instance;
                                                                  })
                                                    .PipeTo(envelop.ProjectionWaiter);
                             });
            Receive<ProcessesTransitComplete>(t =>
                                              {
                                                  //just ignore 
                                              });
        }
        
        public HandlersPipeActor(IMessageProcessor handlersCatalog, string processManagerPipeActorPathPath)
        {
            _handlersCatalog = handlersCatalog;
            Become(() => InitializationBehavior(processManagerPipeActorPathPath));
        }

        public class Project : IMessageMetadataEnvelop
        {
            private IMessageMetadataEnvelop Envelop { get; set; }

            public Project(IActorRef projectionWaiter, IMessageMetadata metadata, params object[] messages)
            {
                Messages = messages.Select(m => new MessageMetadataEnvelop(m, metadata))
                                   .ToArray();
                ProjectionWaiter = projectionWaiter;
                Message = Messages;
                Metadata = metadata;
            }

            public Project(IActorRef projectionWaiter, IMessageMetadataEnvelop envelop)
            {
                Envelop = envelop;
                Message = Envelop.Message;
                Metadata = Envelop.Metadata;
                Messages = new[] {envelop};
                ProjectionWaiter = projectionWaiter;
            }

            public IActorRef ProjectionWaiter { get; }
            public IReadOnlyCollection<IMessageMetadataEnvelop> Messages { get; }

            public object Message { get; }
            public IMessageMetadata Metadata { get; }
        }

        public IStash Stash { get; set; }
    }
}