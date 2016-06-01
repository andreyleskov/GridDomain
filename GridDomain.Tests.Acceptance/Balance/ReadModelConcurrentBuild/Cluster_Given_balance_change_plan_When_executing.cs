﻿using System;
using System.CodeDom;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Unity;
using GridDomain.Balance.Node;
using GridDomain.Node;
using GridDomain.Node.Configuration;
using GridDomain.Tests.Acceptance.Persistence;

namespace GridDomain.Tests.Acceptance.Balance.ReadModelConcurrentBuild
{
    public class Cluster_Given_balance_change_plan_When_executing: Given_balance_change_plan_When_executing
    {
        protected override GridDomainNode GreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig)
        {
            var akkaCluster = ActorSystemFactory.CreateCluster(akkaConf);
            var container = DefaultUnityContainer(dbConfig);

            return new GridDomainNode(container, 
                                      new BalanceCommandsRouting(),
                                      TransportMode.Cluster, akkaCluster.All);
        }

        /// <summary>
        /// Important than persistence setting are the same as for testing cluster as for test ActorSystem
        /// </summary>
        public Cluster_Given_balance_change_plan_When_executing() : base(AkkaConf.ToClusterSeedNodeSystemConfig("writeModelCheckSystem"))
        {
          
        }

        protected override TimeSpan Timeout => TimeSpan.FromSeconds(10);
        protected override int BusinessNum => 1;
        protected override int ChangesPerBusiness => 1;
    }
}