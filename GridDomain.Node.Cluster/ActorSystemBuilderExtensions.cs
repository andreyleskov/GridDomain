using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using GridDomain.Node.Actors.Logging;
using GridDomain.Node.Configuration;
using GridDomain.Node.Configuration.Hocon;
using Serilog.Events;

namespace GridDomain.Node.Cluster {
    public static class ActorSystemBuilderExtensions
    {
        public static ActorSystemBuilder ClusterSeed(this ActorSystemBuilder builder, string name, params INodeNetworkAddress[] otherSeeds)
        {
            builder.Add(new ClusterSeedAwareTransportConfig(otherSeeds.Select(s => s.ToFullTcpAddress(name)).ToArray()));
            builder.Add(new AutoTerminateProcessOnClusterShutdown());
            return builder;
        }

        public static ClusterConfigBuilder Cluster(this ActorSystemBuilder builder, string name)
        {
           // builder.Add(new ClusterInternalMessagesSerializerConfig());
            return new ClusterConfigBuilder(name, builder);
        }  
        
        public static IActorSystemFactory BuildClusterSystemFactory(this ActorSystemBuilder builder, string name)
        {
            Config hocon = new RootConfig(builder.Configs.ToArray()).Build();
            var factory = new HoconActorSystemFactory(name,  hocon.WithFallback(ClusterSingletonManager.DefaultConfig()));
            return factory;
        }
    }
}