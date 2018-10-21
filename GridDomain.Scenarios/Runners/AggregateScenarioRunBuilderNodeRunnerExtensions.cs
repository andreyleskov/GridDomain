using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node;
using GridDomain.Node.Cluster;
using GridDomain.Node.Cluster.Configuration;
using GridDomain.Node.Configuration;
using GridDomain.Node.Logging;
using GridDomain.Node.Persistence.Sql;
using GridDomain.Scenarios.Builders;
using Serilog;
using Serilog.Events;

namespace GridDomain.Scenarios.Runners
{
    public static class AggregateScenarioRunBuilderNodeRunnerExtensions
    {
        public static async Task<IAggregateScenarioRun<TAggregate>> Node<TAggregate>(this IAggregateScenarioRunBuilder<TAggregate> builder,
                                                                                     IDomainConfiguration domainConfig,
                                                                                     ILogger logger,
                                                                                     string sqlPersistenceConnectionString = null)
            where TAggregate : class, IAggregate
        {
            var name = "NodeScenario" + typeof(TAggregate).BeautyName();

            var actorSystemConfigBuilder = new ActorSystemConfigBuilder()
                                           .EmitLogLevel(LogEventLevel.Verbose, true)
                                           .DomainSerialization();
            if (sqlPersistenceConnectionString != null)
                actorSystemConfigBuilder = actorSystemConfigBuilder.SqlPersistence(new DefaultNodeDbConfiguration(sqlPersistenceConnectionString));

            var clusterConfig = actorSystemConfigBuilder
                                .Cluster(name)
                                .AutoSeeds(1)
                                .Build()
                                .Log(s => logger);

            using (var clusterInfo = await clusterConfig.CreateCluster())
            {
                var node = new ClusterNodeBuilder()
                           .ActorSystem(() => clusterInfo.Systems.First())
                           .DomainConfigurations(domainConfig)
                           .Log(logger)
                           .Build();

                await node.Start();

                var runner = new AggregateScenarioNodeRunner<TAggregate>(node);

                return await runner.Run(builder.Scenario);
            }
        }

        public static async Task<IAggregateScenarioRun<TAggregate>> Cluster<TAggregate>(this IAggregateScenarioRunBuilder<TAggregate> builder,
                                                                                        IDomainConfiguration domainConfig,
                                                                                        string sqlPersistenceConnectionString,
                                                                                        Func<LoggerConfiguration> logConfigurationFactory = null,
                                                                                        int autoSeeds = 2,
                                                                                        int workers = 2,
                                                                                        string name = null) where TAggregate : class, IAggregate
        {
            var clusterConfig = new ActorSystemConfigBuilder()
                                .EmitLogLevel(LogEventLevel.Verbose, true)
                                .DomainSerialization()
                                .SqlPersistence(new DefaultNodeDbConfiguration(sqlPersistenceConnectionString))
                                .Cluster(name ?? "ClusterAggregateScenario" + typeof(TAggregate).BeautyName())
                                .AutoSeeds(autoSeeds)
                                .Workers(workers)
                                .Build()
                                .Log(s => (logConfigurationFactory ?? (() => new LoggerConfiguration()))()
                                          .WriteToFile(LogEventLevel.Verbose,
                                                       "GridNodeSystem"
                                                       + s.GetAddress()
                                                          .Port)
                                          .CreateLogger());


            using (var clusterInfo = await clusterConfig.CreateCluster())
            {
                IExtendedGridDomainNode node = null;
                foreach (var system in clusterInfo.Systems)
                {
                    var ext = system.GetExtension<LoggingExtension>();
                    node = new GridNodeBuilder()
                               .Cluster()
                               .ActorSystem(() => system)
                               .DomainConfigurations(domainConfig)
                               .Log(ext.Logger)
                               .Build();

                    await node.Start();
                }
                    
                var runner = new AggregateScenarioNodeRunner<TAggregate>(node);

                var run = await runner.Run(builder.Scenario);

                return run;
            }
        }

        public static async Task<IAggregateScenarioRun<TAggregate>> Cluster<TAggregate>(this IAggregateScenarioRunBuilder<TAggregate> builder,
                                                                                        IDomainConfiguration domainConfig,
                                                                                        Func<LoggerConfiguration> logConfigurationFactory = null,
                                                                                        int autoSeeds = 2,
                                                                                        int workers = 2,
                                                                                        string name = null) where TAggregate : class, IAggregate
        {

            var clusterConfig = new ActorSystemConfigBuilder()
                                .EmitLogLevel(LogEventLevel.Verbose, true)
                                .DomainSerialization()
                                .Cluster(name ?? "ClusterAggregateScenario" + typeof(TAggregate).BeautyName())
                                .AutoSeeds(autoSeeds)
                                .Workers(workers)
                                .Build()
                                .Log(s => (logConfigurationFactory ?? (() => new LoggerConfiguration()))()
                                          .WriteToFile(LogEventLevel.Verbose,
                                                       "GridNodeSystem"
                                                       + s.GetAddress()
                                                          .Port)
                                          .CreateLogger());
                            
            using (var clusterInfo = await clusterConfig.CreateCluster())
            {
                var nodes = new List<IExtendedGridDomainNode>();
                foreach (var system in clusterInfo.Systems)
                {
                    var ext = system.GetExtension<LoggingExtension>();
                    var node = new GridNodeBuilder()
                                                   .Cluster()
                                                   .ActorSystem(() => system)
                                                   .DomainConfigurations(domainConfig)
                                                   .Log(ext.Logger)
                                                   .Build();

                    await node.Start();
                    nodes.Add(node);
                }
                    
                var runner = new AggregateScenarioClusterInMemoryRunner<TAggregate>(nodes);

                var run = await runner.Run(builder.Scenario);

                return run;
            }
        }
    }
}