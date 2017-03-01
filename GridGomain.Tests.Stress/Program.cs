﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.CQRS;
using GridDomain.Node;
using GridDomain.Node.Actors;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.Framework.Configuration;
using GridDomain.Tests.XUnit.SampleDomain;
using GridDomain.Tests.XUnit.SampleDomain.Commands;
using GridDomain.Tests.XUnit.SampleDomain.Events;
using Microsoft.Practices.Unity;

namespace GridGomain.Tests.Stress
{
    public class Program
    {
        public static void Main(params string[] args)
        {
            //RawCommandExecution(1, 1000, 20);

            var system = ActorSystem.Create("test");
            var actor = system.ActorOf(Props.Create(() => new TestLogActor()));
            actor.Ask<string>("hi").Wait();

            Console.WriteLine("Sleeping");
            Console.ReadKey();
        }

        private static void RawCommandExecution(int totalAggregateScenariosCount,
                                                int aggregateScenarioPackSize,
                                                int aggregateChangeAmount)
        {
            var dbCfg = new AutoTestAkkaConfiguration();

            using (var connection = new SqlConnection(dbCfg.Persistence.JournalConnectionString))
            {
                connection.Open();
                var sqlText = @"TRUNCATE TABLE Journal";
                var cmdJournal = new SqlCommand(sqlText, connection);
                cmdJournal.ExecuteNonQuery();

                var sqlText1 = @"TRUNCATE TABLE Snapshots";
                var cmdSnapshots = new SqlCommand(sqlText1, connection);
                cmdSnapshots.ExecuteNonQuery();
            }

            var unityContainer = new UnityContainer();
            unityContainer.Register(new SampleDomainContainerConfiguration());

            var cfg = new CustomContainerConfiguration(c => c.Register(new SampleDomainContainerConfiguration()),
                                                       c => c.RegisterType<IPersistentChildsRecycleConfiguration, InsertOptimazedBulkConfiguration>(),
                                                       c => c.RegisterType<IQuartzConfig, PersistedQuartzConfig>());

            Func<ActorSystem[]> actorSystemFactory = () => new[] {new StressTestAkkaConfiguration().CreateSystem()};

            var settings = new NodeSettings(cfg, new SampleRouteMap(), actorSystemFactory);
            var node = new GridDomainNode(settings);

            node.Start().Wait();

            var timer = new Stopwatch();
            timer.Start();

            var timeoutedCommads = 0;
            var random = new Random();
            var commandsInScenario = aggregateScenarioPackSize * (aggregateChangeAmount + 1);
            var totalCommandsToIssue = commandsInScenario * totalAggregateScenariosCount;


            for (var i = 0; i < totalAggregateScenariosCount; i++)
            {
                var packTimer = new Stopwatch();
                packTimer.Start();
                var tasks =
                    Enumerable.Range(0, aggregateScenarioPackSize)
                              .Select(t => WaitAggregateCommands(aggregateChangeAmount, random, node))
                              .ToArray();
                try
                {
                    Task.WhenAll(tasks).Wait();
                }
                catch
                {
                    timeoutedCommads += tasks.Count(t => t.IsCanceled || t.IsFaulted);
                }

                packTimer.Stop();
                var speed = (decimal) (commandsInScenario / packTimer.Elapsed.TotalSeconds);
                var timeLeft = TimeSpan.FromSeconds((double) ((totalCommandsToIssue - i * commandsInScenario) / speed));

                Console.WriteLine($"speed :{speed} cmd/sec," + $"total errors: {timeoutedCommads}, "
                                  + $"total commands executed: {i * commandsInScenario}/{totalCommandsToIssue},"
                                  + $"approx time remaining: {timeLeft}");
            }


            timer.Stop();
            node.Stop().Wait();

            var speedTotal = (decimal) (totalCommandsToIssue / timer.Elapsed.TotalSeconds);
            Console.WriteLine(
                              $"Executed {totalAggregateScenariosCount} batches = {totalCommandsToIssue} commands in {timer.Elapsed}");
            Console.WriteLine($"Average speed was {speedTotal} cmd/sec");

            using (var connection = new SqlConnection(dbCfg.Persistence.JournalConnectionString))
            {
                connection.Open();
                var sqlText = @"SELECT COUNT(*) FROM Journal";
                var cmdJournal = new SqlCommand(sqlText, connection);
                var count = (int) cmdJournal.ExecuteScalar();

                Console.WriteLine(count == totalCommandsToIssue
                                      ? "Journal contains all events"
                                      : $"Journal contains only {count} of {totalCommandsToIssue}");
            }
        }

        private static GridDomainNode StartSampleDomainNode()
        {
            var unityContainer = new UnityContainer();
            unityContainer.Register(new SampleDomainContainerConfiguration());

            var cfg = new CustomContainerConfiguration(c => c.Register(new SampleDomainContainerConfiguration()),
                                                       c => c.RegisterType<IPersistentChildsRecycleConfiguration, InsertOptimazedBulkConfiguration>(),
                                                       c => c.RegisterType<IQuartzConfig, PersistedQuartzConfig>());

            Func<ActorSystem[]> actorSystemFactory = () => new[] {new StressTestAkkaConfiguration().CreateSystem()};

            var settings = new NodeSettings(cfg, new SampleRouteMap(), actorSystemFactory);
            var node = new GridDomainNode(settings);

            node.Start().Wait();
            return node;
        }

        private static AutoTestAkkaConfiguration ClearWriteDb()
        {
            var dbCfg = new AutoTestAkkaConfiguration();

            using (var connection = new SqlConnection(dbCfg.Persistence.JournalConnectionString))
            {
                connection.Open();
                var cmdJournal = new SqlCommand(@"TRUNCATE TABLE Journal", connection);
                cmdJournal.ExecuteNonQuery();

                var cmdSnapshots = new SqlCommand(@"TRUNCATE TABLE Snapshots", connection);
                cmdSnapshots.ExecuteNonQuery();
            }
            return dbCfg;
        }

        private static async Task WaitAggregateCommands(int changeNumber, Random random, GridDomainNode node)
        {
            await
                node.Prepare(new CreateSampleAggregateCommand(random.Next(), Guid.NewGuid()))
                    .Expect<SampleAggregateCreatedEvent>()
                    .Execute();

            for (var num = 0; num < changeNumber; num++)
                await
                    node.Prepare(new ChangeSampleAggregateCommand(random.Next(),
                                                                  new CreateSampleAggregateCommand(random.Next(), Guid.NewGuid()).AggregateId))
                        .Expect<SampleAggregateChangedEvent>()
                        .Execute();
        }
    }
}