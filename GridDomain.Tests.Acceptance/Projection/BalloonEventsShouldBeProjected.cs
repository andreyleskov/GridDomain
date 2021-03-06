﻿using System;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Tests.Acceptance.BalloonDomain;
using GridDomain.Tests.Acceptance.Snapshots;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.ProjectionBuilders;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.Projection
{
    public class BalloonEventsShouldBeProjected : NodeTestKit
    {
        protected BalloonEventsShouldBeProjected(NodeTestFixture fixture) : base(fixture) { }

        public BalloonEventsShouldBeProjected(ITestOutputHelper output) :
            base(new BalloonWithProjectionFixture(output,
                                                  new DbContextOptionsBuilder<BalloonContext>()
                                                      .UseSqlServer(ConnectionStrings.AutoTestDb)
                                                      .Options).UseSqlPersistence()) { }

        [Fact]
        public async Task When_Executing_command_events_should_be_projected()
        {
            var autoTestDb = ConnectionStrings.AutoTestDb;

            var dbOptions = new DbContextOptionsBuilder<BalloonContext>().UseSqlServer(autoTestDb)
                                                                         .Options;
            //warm up EF 
            using (var context = new BalloonContext(dbOptions))
            {
                context.BalloonCatalog.Add(new BalloonCatalogItem()
                                           {
                                               BalloonId = Guid.NewGuid()
                                                               .ToString(),
                                               LastChanged = DateTime.UtcNow,
                                               Title = "WarmUp"
                                           });
                await context.SaveChangesAsync();
            }

            await TestDbTools.Truncate(autoTestDb, "BalloonCatalog");

            var cmd = new InflateNewBallonCommand(123,
                                                  Guid.NewGuid()
                                                      .ToString());
            await Node.Prepare(cmd)
                      .Expect<BalloonCreatedNotification>()
                      .Execute(TimeSpan.FromSeconds(30));

            using (var context = new BalloonContext(dbOptions))
            {
                var catalogItem = await context.BalloonCatalog.FindAsync(cmd.AggregateId);
                Assert.Equal(cmd.Title.ToString(), catalogItem.Title);
            }
        }
    }
}