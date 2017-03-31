using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using GridDomain.Common;
using GridDomain.Node.AkkaMessaging;

namespace GridDomain.Node.Actors
{
    /// <summary>
    ///     Any child should be terminated by ShutdownRequest message
    /// </summary>
    public abstract class PersistentHubActor : ReceiveActor,
                                               IWithUnboundedStash
    {
        private readonly ProcessEntry _forwardEntry;
        private readonly ActorMonitor _monitor;
        private readonly IPersistentChildsRecycleConfiguration _recycleConfiguration;
        internal readonly IDictionary<Guid, ChildInfo> Children = new Dictionary<Guid, ChildInfo>();
        private readonly ILoggingAdapter Logger = Context.GetLogger();

        protected PersistentHubActor(IPersistentChildsRecycleConfiguration recycleConfiguration, string counterName)
        {
            _recycleConfiguration = recycleConfiguration;
            _monitor = new ActorMonitor(Context, $"Hub_{counterName}");
            _forwardEntry = new ProcessEntry(Self.Path.Name, "Forwarding to child", "All messages should be forwarded");

            Receive<Terminated>(t =>
                                {
                                    Guid id;
                                    if (!AggregateActorName.TryParseId(t.ActorRef.Path.Name, out id))
                                        return;
                                    Children.Remove(id);
                                    //continue to process any remaining messages
                                    //for example when we are trying to resume terminating child with no success
                                    Stash.UnstashAll();
                                });
            Receive<ClearChildren>(m => Clear());
            Receive<ShutdownChild>(m => ShutdownChild(m.ChildId));
            Receive<ShutdownCanceled>(m =>
                                      {
                                          Guid id;
                                          if (!AggregateActorName.TryParseId(Sender.Path.Name, out id))
                                              return;
                                          //child was resumed from planned shutdown
                                          Children[id].Terminating = false;
                                          Stash.UnstashAll();
                                          Logger.Debug("Child {id} resumed. Stashed messages will be sent to it", id);
                                      });
            Receive<CheckHealth>(s => Sender.Tell(new HealthStatus(s.Payload)));
            Receive<IMessageMetadataEnvelop>(messageWithMetadata =>
                                             {
                                                 var childId = GetChildActorId(messageWithMetadata);
                                                 var name = GetChildActorName(messageWithMetadata, childId);
                                                 SendToChild(messageWithMetadata, childId, name);
                                             });
        }

        protected virtual void SendToChild(IMessageMetadataEnvelop messageWithMetadata, Guid childId, string name)
        {
            ChildInfo knownChild;
            messageWithMetadata.Metadata.History.Add(_forwardEntry);

            var childWasCreated = false;
            if (!Children.TryGetValue(childId, out knownChild))
            {
                childWasCreated = true;
                knownChild = CreateChild(messageWithMetadata, name);
                Children[childId] = knownChild;
                Context.Watch(knownChild.Ref);
            }
            else
            {
                //terminating a child is quite long operation due to snapshots saving
                //it is cheaper to resume child than wait for it termination and create rom scratch
                if (knownChild.Terminating)
                {
                    Stash.Stash();
                    knownChild.Ref.Tell(CancelShutdownRequest.Instance);
                    Logger.Debug(
                                 "Stashing message {msg} for child {id}. Waiting for child resume from termination",
                                 messageWithMetadata,
                                 childId);

                    return;
                }
            }

            knownChild.LastTimeOfAccess = BusinessDateTime.UtcNow;
            knownChild.ExpiresAt = knownChild.LastTimeOfAccess + ChildMaxInactiveTime;
            SendMessageToChild(knownChild, messageWithMetadata);

            Logger.Debug("Message {msg} sent to {isknown} child {id}",
                         messageWithMetadata,
                         childWasCreated ? "new" : "known",
                         childId);
        }

        //TODO: replace with more efficient implementation
        internal virtual TimeSpan ChildClearPeriod => _recycleConfiguration.ChildClearPeriod;
        internal virtual TimeSpan ChildMaxInactiveTime => _recycleConfiguration.ChildMaxInactiveTime;
        public IStash Stash { get; set; }

        protected abstract string GetChildActorName(IMessageMetadataEnvelop message, Guid childId);
        protected abstract Guid GetChildActorId(IMessageMetadataEnvelop message);
        protected abstract Type GetChildActorType(IMessageMetadataEnvelop message);

        protected virtual void SendMessageToChild(ChildInfo knownChild, IMessageMetadataEnvelop message)
        {
            knownChild.Ref.Tell(message);
        }

        private void Clear()
        {
            var now = BusinessDateTime.UtcNow;
            var childsToTerminate =
                Children.Where(c => now > c.Value.ExpiresAt && !c.Value.Terminating).Select(ch => ch.Key).ToArray();

            foreach (var childId in childsToTerminate)
                ShutdownChild(childId);

            Logger.Debug("Clear childs process finished, removing {childsToTerminate} childs", childsToTerminate.Length);
        }

        private void ShutdownChild(Guid childId)
        {
            ChildInfo childInfo;
            if (!Children.TryGetValue(childId, out childInfo))
                return;

            childInfo.Ref.Tell(GracefullShutdownRequest.Instance);
            childInfo.Terminating = true;
        }

        protected override bool AroundReceive(Receive receive, object message)
        {
            _monitor.IncrementMessagesReceived();
            return base.AroundReceive(receive, message);
        }

        protected virtual ChildInfo CreateChild(IMessageMetadataEnvelop messageWitMetadata, string name)
        {
            var childActorType = GetChildActorType(messageWitMetadata);
            var props = Context.DI().Props(childActorType);
            var childActorRef = Context.ActorOf(props, name);
            return new ChildInfo(childActorRef);
        }

        protected override void PreStart()
        {
            _monitor.IncrementActorStarted();
            Logger.Debug("{ActorHub} is going to start", Self.Path);
            Context.System.Scheduler.ScheduleTellRepeatedly(ChildClearPeriod,
                                                            ChildClearPeriod,
                                                            Self,
                                                            new ClearChildren(),
                                                            Self);
        }

        protected override void PostStop()
        {
            _monitor.IncrementActorStopped();
            Logger.Debug("{ActorHub} was stopped", Self.Path);
        }

        public class ClearChildren {}
    }
}