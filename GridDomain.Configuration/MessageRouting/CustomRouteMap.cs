using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GridDomain.Configuration.MessageRouting
{
    public class CustomRouteMap : IMessageRouteMap
    {
        private readonly List<Func<IMessagesRouter, Task>> _routeRules;

        public CustomRouteMap(params Func<IMessagesRouter, Task>[] routeRules)
        {
            if (routeRules == null || !routeRules.Any())
                throw new EmptyRouteMapException();

             _routeRules = routeRules.ToList();
        }

        class
            EmptyRouteMapException : Exception { }

        public CustomRouteMap(string name, params Func<IMessagesRouter, Task>[] routeRules):this(routeRules)
        {
            Name = name;
        }

        public async Task Register(IMessagesRouter router)
        {
            foreach (var routeRule in _routeRules)
                await routeRule(router);
        }

        public string Name { get; }
    }
}