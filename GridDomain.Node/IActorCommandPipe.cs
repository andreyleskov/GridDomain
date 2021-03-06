using System;
using Akka.Actor;
using GridDomain.Configuration.MessageRouting;
using GridDomain.Node.Configuration;

namespace GridDomain.Node {
    public interface IActorCommandPipe: IMessagesRouter, IDisposable
    {
        IActorRef ProcessesPipeActor { get; }
        IActorRef HandlersPipeActor { get; }
        IActorRef CommandExecutor { get; }

        IContainerConfiguration Prepare();
    }
}