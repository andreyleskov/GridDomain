using System;
using System.Collections.Generic;
using GridDomain.EventSourcing;

namespace GridDomain.Configuration.MessageRouting
{
    public class AggregateCommandsHandlerDescriptor<T> : IAggregateCommandsHandlerDescriptor
    {
        private readonly List<Type> _registrations = new List<Type>();
        public IReadOnlyCollection<Type> RegisteredCommands => _registrations;
        public Type AggregateType => typeof(T);

        public void RegisterCommand<TCommand>()
        {
            RegisterCommand(typeof(TCommand));
        }

        public void RegisterCommand(Type type)
        {
            _registrations.Add(type);
        }
    }
}