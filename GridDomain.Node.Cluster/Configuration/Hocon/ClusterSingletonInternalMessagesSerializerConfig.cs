using Akka.Configuration;
using GridDomain.Node.Configuration.Hocon;

namespace GridDomain.Node.Cluster.Configuration.Hocon {
    public class ClusterSingletonInternalMessagesSerializerConfig : IHoconConfig
    {
        public string Build()
        {
            return @"akka.actor : {
                                 serializers : {
                                     akka-singleton : ""Akka.Cluster.Tools.Singleton.Serialization.ClusterSingletonMessageSerializer, Akka.Cluster.Tools""
                                 }
                                 serialization-bindings : {
                                     ""Akka.Cluster.Tools.Singleton.IClusterSingletonMessage, Akka.Cluster.Tools"" : akka-singleton
                                 }
                                 serialization-identifiers : {
                                     ""Akka.Cluster.Tools.Singleton.Serialization.ClusterSingletonMessageSerializer, Akka.Cluster.Tools"" : 14
                                 }
                             }
                    ";
        }
    }
}