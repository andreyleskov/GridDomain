using System;
using System.Collections.Generic;
using CommonDomain;

namespace GridDomain.EventSourcing.VersionedTypeSerialization
{
    public class DomainEventsUpgradeChain
    {
        private readonly IDictionary<Type, IDomainEventAdapter> _adapterCatalog = new Dictionary<Type, IDomainEventAdapter>();

        public object[] Update(object evt)
        {
            IDomainEventAdapter adapter = null;
            var processingType = evt.GetType();
            List<object> updatedEvent =  new List<object>{evt};

            while (_adapterCatalog.TryGetValue(processingType, out adapter))
            {
                var eventsToProcess = updatedEvent.ToArray();
                processingType = adapter.Descriptor.To;

                updatedEvent.Clear();
                foreach(var ev in eventsToProcess)
                    updatedEvent.AddRange(adapter.Convert(ev));
            }

            return updatedEvent.ToArray();
        }

        /// <summary>
        /// All types should form a chain 
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="adapter"></param>
        public void Register<TFrom, TTo>(IDomainEventAdapter<TFrom, TTo> adapter)
        {
            _adapterCatalog[typeof(TFrom)] = adapter;
        }
    }
}