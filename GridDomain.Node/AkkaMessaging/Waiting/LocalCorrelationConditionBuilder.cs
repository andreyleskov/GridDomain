using System;
using GridDomain.Common;

namespace GridDomain.Node.AkkaMessaging.Waiting {
    public class LocalCorrelationConditionBuilder : MetadataEnvelopConditionBuilder
    {
        private readonly string _correlationId;

        public LocalCorrelationConditionBuilder(string correlationId)
        {
            _correlationId = correlationId;
        }
        protected override Func<object, bool> AddFilter(Type messageType, Func<object, bool> filter = null)
        {
            AcceptedMessageTypes.Add(typeof(MessageMetadataEnvelop));

            bool FilterWithAdapter(object o) => 
                o.SafeCheckCorrelation(_correlationId) && CheckMessageType(o, messageType, filter);
            
            MessageFilters.Add(FilterWithAdapter);
            return FilterWithAdapter;
        }
    }
}