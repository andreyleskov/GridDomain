using System;
using GridDomain.Configuration;

namespace GridDomain.Node.Actors.PersistentHub
{
    public class DefaultPersistentChildsRecycleConfiguration : IPersistentChildsRecycleConfiguration
    {
        public TimeSpan ChildClearPeriod { get; } = TimeSpan.FromMinutes(5);
        public TimeSpan ChildMaxInactiveTime { get; } = TimeSpan.FromMinutes(10);
    }
}