using Akka.Configuration;
using GridDomain.Node.Configuration.Hocon;

namespace GridDomain.Node.Cluster.Configuration.Hocon {
    public class AutoTerminateProcessOnClusterShutdown : IHoconConfig
    {
        public string Build()
        {
            return "akka.cluster.coordinated-shutdown.exit-clr = on";
        }
    }
}