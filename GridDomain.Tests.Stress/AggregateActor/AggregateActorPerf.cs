using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.Aggregates.Messages;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.EventSourced;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Node.Serializers;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Xunit.Abstractions;
using GridDomain.Transport.Extension;
using Serilog.Events;

namespace GridDomain.Tests.Stress.AggregateActor {
    //it is performance test, not pure xunit
#pragma warning disable xUnit1013
    public abstract class AggregateActorPerf
    {
        private readonly string _actorSystemConfig;
        private const string TotalCommandsExecutedCounter = "TotalCommandsExecutedCounter";
        private Counter _counter;
        private IActorRef _aggregateActor;
        private ICommand[] _commands;
        private readonly ITestOutputHelper _testOutputHelper;
        private ActorSystem _actorSystem;

        protected AggregateActorPerf(ITestOutputHelper output,string actorSystemConfig)
        {
            _actorSystemConfig = actorSystemConfig;
            _testOutputHelper = output;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
            
        }

        protected virtual ActorSystem CreateActorSystem()
        {
             return ActorSystem.Create("PerfTest",_actorSystemConfig);
        }

        class CustomHandlersActorDummy : ReceiveActor
        {
            public CustomHandlersActorDummy()
            {
                Receive<IMessageMetadataEnvelop>(m => Sender.Tell(AllHandlersCompleted.Instance));
            }
        }

        private IEnumerable<ICommand> CreateAggregatePlan(int changeAmount, string aggregateId)
        {
            var random = new Random();
            yield return new InflateNewBallonCommand(random.Next(), aggregateId);
            for (var num = 0; num < changeAmount; num++)
                yield return new WriteTitleCommand(random.Next(), aggregateId);
        }

        [PerfSetup]
        public virtual void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter(TotalCommandsExecutedCounter);
            var aggregateId = Guid.NewGuid().ToString();
            
            _commands = CreateAggregatePlan(10, aggregateId).ToArray();


            _actorSystem = CreateActorSystem();
            
            //var statsDmonitor = new ActorStatsDMonitor("localhost",32768,GetType().Name);
            //
            //var registeredMonitor = ActorMonitoringExtension.RegisterMonitor(_actorSystem, 
            //                                                                 statsDmonitor); //new ActorAppInsightsMonitor(), new ActorPerformanceCountersMonitor ();

            _actorSystem.InitLocalTransportExtension();
            _actorSystem.InitDomainEventsSerialization(new EventsAdaptersCatalog());
            var log = new XUnitAutoTestLoggerConfiguration(_testOutputHelper,LogEventLevel.Warning,GetType().Name).CreateLogger();
            
            _actorSystem.AttachSerilogLogging(log);
            
            var dummy = _actorSystem.ActorOf<CustomHandlersActorDummy>();
            
            _aggregateActor = _actorSystem.ActorOf(Props.Create(
                                                       () => new AggregateActor<Balloon>(new EachMessageSnapshotsPersistencePolicy(), 
                                                                                         AggregateFactory.Default,
                                                                                         AggregateFactory.Default,
                                                                                         dummy)),
                                          EntityActorName.New<Balloon>(aggregateId).ToString());
        }

        [NBenchFact]
        [PerfBenchmark(Description = "Measuring command executions directly by aggregate actor",
            NumberOfIterations = 10,
            RunMode = RunMode.Iterations,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion(TotalCommandsExecutedCounter, MustBe.GreaterThan, 10)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        public void MeasureCommandExecution_until_projection()
        {
            foreach (var command in _commands)
            {
                _aggregateActor.Ask<Node.Actors.Aggregates.AggregateActor.CommandProjected>(new MessageMetadataEnvelop<ICommand>(command, MessageMetadata.Empty))
                               .ContinueWith(c =>   _counter.Increment()).Wait();
            }
        }
        
        [NBenchFact]
        [PerfBenchmark(Description = "Measuring command executions directly by aggregate actor, without projection",
            NumberOfIterations = 10,
            RunMode = RunMode.Iterations,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion(TotalCommandsExecutedCounter, MustBe.GreaterThan, 10)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        public void MeasureCommandExecution_without_projection()
        {
            Task.WhenAll(_commands.Select(c => _aggregateActor.Ask<Node.Actors.Aggregates.AggregateActor.CommandExecuted>(new MessageMetadataEnvelop<ICommand>(c, MessageMetadata.Empty))
                                                              .ContinueWith(t => _counter.Increment())))
                .Wait();
        }

        [PerfCleanup]
        public void Cleanup()
        {
            _actorSystem.Terminate()
                        .Wait();
        }
    }
}