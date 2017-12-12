using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Akka.Actor;
using Akka.Persistence;
using Automatonymous;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.Configuration.MessageRouting;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors.Aggregates.Messages;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.EventSourced;
using GridDomain.Node.Actors.EventSourced.Messages;
using GridDomain.Node.Actors.ProcessManagers.Messages;
using GridDomain.Transport.Extension;

namespace GridDomain.Node.Actors.Aggregates
{



    public class CommandStateActor : ReceivePersistentActor
    {
        enum CommandState
        {
            New,
            Executed,
            Failed
        }

        private CommandState State;
        public CommandStateActor()
        {
            State = CommandState.New;
            PersistenceId = Self.Path.Name;
            Command<AcceptCommandExecution>(a =>
                                            {
                                                if(State != CommandState.Executed)
                                                    Sender.Tell(Accepted.Instance);
                                                else 
                                                   Sender.Tell(Rejected.Instance);
                                            });
            Command<CommandSucceed>(s => Persist(CommandState.Executed, e => State = e));
            Command<CommandFailed>(s => Persist(CommandState.Failed, e => State = e));
            Recover<CommandState>(s => State = s);
        }

        public class AcceptCommandExecution
        {
            private AcceptCommandExecution()
            {
                
            }
             public static AcceptCommandExecution Instance { get; } = new AcceptCommandExecution();
        }

        public class Accepted
        {
            private Accepted()
            {
                
            }
            public static Accepted Instance { get; } = new Accepted();
        }

        public class Rejected
        {
            private Rejected()
            {
                
            }
            public static Rejected Instance { get; } = new Rejected();
        }

        public class CommandFailed
        {
            private CommandFailed(){}
            public static CommandFailed Instance { get; } = new CommandFailed();
        }
        public class CommandSucceed
        {
            private CommandSucceed(){}
            public static CommandSucceed Instance { get; } = new CommandSucceed();
        }

        public override string PersistenceId { get; }
    }
    
    public class CommandAlreadyExecutedException:Exception { }

    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public class AggregateActor<TAggregate> : DomainEventSourcedActor<TAggregate> where TAggregate : class,IAggregate
    {
        
        private readonly IActorRef _customHandlersActor;
        private readonly ProcessEntry _domainEventProcessEntry;
        private readonly ProcessEntry _domainEventProcessFailEntry;
        private readonly ProcessEntry _commandCompletedProcessEntry;
        private readonly IPublisher _publisher;

        private readonly IAggregateCommandsHandler<TAggregate> _aggregateCommandsHandler;
        private PersistenceActorEventStore<EventsPersisted> EventStore;
        private AggregateCommandExecutionContext ExecutionContext { get; } = new AggregateCommandExecutionContext();

        public AggregateActor(IAggregateCommandsHandler<TAggregate> handler,
                              ISnapshotsPersistencePolicy snapshotsPersistencePolicy,
                              IConstructAggregates aggregateConstructor,
                              IActorRef customHandlersActor) : base(aggregateConstructor, snapshotsPersistencePolicy)
        {
            _aggregateCommandsHandler = handler;
            _publisher = Context.System.GetTransport();
            _customHandlersActor = customHandlersActor;
            _domainEventProcessEntry = new ProcessEntry(Self.Path.Name, AggregateActorConstants.PublishingEvent, AggregateActorConstants.CommandExecutionCreatedAnEvent);
            _domainEventProcessFailEntry = new ProcessEntry(Self.Path.Name, AggregateActorConstants.CommandExecutionFinished, AggregateActorConstants.CommandRaisedAnError);
            _commandCompletedProcessEntry = new ProcessEntry(Self.Path.Name, AggregateActorConstants.CommandExecutionFinished, AggregateActorConstants.ExecutedCommand);
            Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
            EventStore = new PersistenceActorEventStore<EventsPersisted>(Self,ExecutionContext); 
        }


        protected void ValidatingCommandBehavior()
        {
            DefaultBehavior();
            Command<IMessageMetadataEnvelop>(m =>
                                             {
                                                 var cmd = (ICommand)m.Message;
                                                 var name = cmd.Id.ToString();

                                                 var actorRef = Context.Child(name);
                                                 ExecutionContext.Validator = actorRef != ActorRefs.Nobody ? actorRef : Context.ActorOf<CommandStateActor>(name);
                                                 ExecutionContext.Command = cmd;
                                                 ExecutionContext.CommandMetadata = m.Metadata;
                                                 ExecutionContext.CommandSender = Sender;
                                                 ExecutionContext.Validator.Tell(CommandStateActor.AcceptCommandExecution.Instance);
                                                 
                                             }, m => m.Message is ICommand);
            
            Command<CommandStateActor.Accepted>(a =>
                                                {
                                                    Behavior.Become(ProcessingCommandBehavior, nameof(ProcessingCommandBehavior));

                                                    Log.Debug("Executing command. {@m}", ExecutionContext);

                                                    _aggregateCommandsHandler.ExecuteAsync(State,
                                                                                           ExecutionContext.Command,
                                                                                           EventStore)
                                                                             .ContinueWith(t =>
                                                                                           {
                                                                                               if (t.IsFaulted)
                                                                                                   throw new CommandExecutionFailedException(ExecutionContext.Command, t.Exception.UnwrapSingle());
                                                                                               return CommandExecuted.Instance;
                                                                                           })
                                                                             .PipeTo(Self);
                                                });
            Command<CommandStateActor.Rejected>(a =>
                                                {
                                                    PublishError(new CommandAlreadyExecutedException());
                                                    Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
                                                    Stash.UnstashAll();
                                                });
            CommandAny(StashMessage);
        }

        protected virtual void AwaitingCommandBehavior()
        {
            DefaultBehavior();
            
            Command<IMessageMetadataEnvelop>(m =>
                                            {
                                                var cmd = (ICommand)m.Message;
                                                Monitor.Increment(nameof(CQRS.Command));
                                                Behavior.Become(ValidatingCommandBehavior,nameof(ValidatingCommandBehavior));
                                                
                                            }, m => m.Message is ICommand);
        }

        private void ProcessingCommandBehavior()
        {
            var producedEventsMetadata = ExecutionContext.CommandMetadata.CreateChild(Id, _domainEventProcessEntry);
            
            //just for catching Failures on events persist
            Command<IReadOnlyCollection<DomainEvent>>(domainEvents =>
                                   {

                                       Monitor.Increment(nameof(PersistEventPack));
                                     
                                       if (!domainEvents.Any())
                                       {
                                           Log.Warning("Trying to persist events but no events is presented. {@context}", ExecutionContext);
                                           return;
                                       }
                                       ExecutionContext.PersistenceWaiter = Sender;
                                       ExecutionContext.MessagesToProject += domainEvents.Count;

                                       //dirty hack, but we know nobody will modify domain events before us 
                                       foreach (var evt in domainEvents)
                                           evt.ProcessId = ExecutionContext.Command.ProcessId;

                                       PersistAll(domainEvents,
                                                  persistedEvent =>
                                                  {
                                                      try
                                                      {
                                                          ExecutionContext.ProducedState.Commit(persistedEvent);
                                                      }
                                                      catch (Exception ex)
                                                      {
                                                          Log.Error(AggregateActorConstants.ErrorOnEventApplyText, Id, ExecutionContext.Command);
                                                          PublishError(ex);
                                                          //intentionally drop all pending commands and messages
                                                          //and don't wait end of projection builders processing as
                                                          //state is corrupted
                                                          Context.Stop(Self);
                                                          return;
                                                      }

                                                      NotifyPersistenceWatchers(persistedEvent);
                                                      SaveSnapshot(ExecutionContext.ProducedState, persistedEvent);
                                                      Project(persistedEvent, producedEventsMetadata);
                                                  });
                                   });

            Command<AllHandlersCompleted>(c =>
                                          {
                                              if (ExecutionContext.Exception == null)
                                              {
                                                  ExecutionContext.MessagesToProject--;

                                                  if (ExecutionContext.ProducedState.HasUncommitedEvents || ExecutionContext.MessagesToProject > 0)
                                                      return;

                                                  ExecutionContext.PersistenceWaiter.Tell(EventsPersisted.Instance);
                                              }
                                              else
                                              {
                                                  Self.Tell(CommandExecuted.Instance);
                                              }
                                          });
            //aggregate raised an error during command execution
            Command<Status.Failure>(f =>
                                    {
                                        ExecutionContext.Exception = f.Cause.UnwrapSingle();
                                        PublishError(ExecutionContext.Exception);
                                    });

            Command<CommandExecuted>(c =>
                                     {
                                         Log.Debug("Command executed. {@context}", ExecutionContext.CommandMetadata);
                                         //finish command execution. produced state can be null on execution error
                                         State = (TAggregate)ExecutionContext.ProducedState ?? State;
                                         var completedMetadata = ExecutionContext.CommandMetadata
                                                                                 .CreateChild(ExecutionContext.Command.Id, _commandCompletedProcessEntry);

                                         _publisher.Publish(c, completedMetadata);

                                         ExecutionContext.CommandSender.Tell(c);
                                         ExecutionContext.Clear();
                                         Behavior.Unbecome();
                                         Stash.Unstash();
                                     });

            Command<IMessageMetadataEnvelop>(o => StashMessage(o));
            Command<GracefullShutdownRequest>(o => StashMessage(o));

            DefaultBehavior();
        }

        private IFault PublishError(Exception exception)
        {
            var command = ExecutionContext.Command;

            Log.Error(exception, "An error occured while command execution. {@context}", ExecutionContext);

            var producedFaultMetadata = ExecutionContext.CommandMetadata.CreateChild(command.Id, _domainEventProcessFailEntry);
            var fault = Fault.NewGeneric(command, exception, command.ProcessId, typeof(TAggregate));
            Project(fault, producedFaultMetadata);
            ExecutionContext.CommandSender.Tell(fault);
            return fault;
        }

        private void Project(object evt, IMessageMetadata commandMetadata)
        {
            _customHandlersActor.Tell(new MessageMetadataEnvelop(evt, commandMetadata));
        }
        
    }
}