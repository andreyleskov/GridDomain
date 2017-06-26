using Microsoft.Practices.Unity;
using Quartz;

namespace GridDomain.Tests.Unit.FutureEvents
{
    public static class NodeTestFixtureExtensions
    {
        public static void ClearSheduledJobs(this NodeTestFixture fixture)
        {
            fixture.OnNodeStartedEvent += (sender, args) => fixture.Node.Container.Resolve<IScheduler>().Clear();
        }
    }
}