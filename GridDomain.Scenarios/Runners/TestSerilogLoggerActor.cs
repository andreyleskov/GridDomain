using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Event;
using GridDomain.Node;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Cluster;
using GridDomain.Node.Logging;
using Serilog.Events;

namespace GridDomain.Scenarios.Runners {
    public class TestSerilogLoggerActor : SerilogLoggerActor
    {
        private static int _counter = 0;


        public TestSerilogLoggerActor():base(new DefaultLoggerConfiguration(LogEventLevel.Verbose,
                                                                            Path.Combine("StartLogs", (++_counter).ToString())).CreateLogger())
        {
            
        }
       
    }
}