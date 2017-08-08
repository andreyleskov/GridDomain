using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;

using GridDomain.Node.Transports;
using Serilog;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    public class LocalMessagesWaiter<T> : IMessageWaiter<T>
    {
        private readonly ConcurrentBag<object> _allExpectedMessages = new ConcurrentBag<object>();
        private readonly TimeSpan _defaultTimeout;
        internal readonly ConditionBuilder<T> ConditionBuilder;
        private readonly IActorSubscriber _subscriber;
        private readonly ActorSystem _system;

        public LocalMessagesWaiter(ActorSystem system, IActorSubscriber subscriber, TimeSpan defaultTimeout, ConditionBuilder<T> conditionBuilder)
        {
            _system = system;
            _defaultTimeout = defaultTimeout;
            _subscriber = subscriber;
            ConditionBuilder = conditionBuilder;
        }

        public IConditionBuilder<T> Expect<TMsg>(Predicate<TMsg> filter = null)
        {
            return ConditionBuilder.And(filter);
        }

        public IConditionBuilder<T> Expect(Type type, Func<object, bool> filter)
        {
            return ConditionBuilder.And(type, filter);
        }

        public async Task<IWaitResult> Start(TimeSpan? timeout = null)
        {
            if (!_allExpectedMessages.IsEmpty)
                throw new WaiterIsFinishedException();

            using (var inbox = Inbox.Create(_system))
            {
                foreach (var type in ConditionBuilder.MessageFilters.Keys)
                    _subscriber.Subscribe(type, inbox.Receiver);

                var finalTimeout = timeout ?? _defaultTimeout;

                await WaitForMessages(inbox, finalTimeout).TimeoutAfter(finalTimeout);

                foreach (var type in ConditionBuilder.MessageFilters.Keys)
                    _subscriber.Unsubscribe(inbox.Receiver, type);

                return new WaitResult(_allExpectedMessages);
            }
        }

        private async Task WaitForMessages(Inbox inbox, TimeSpan timeoutPerMessage)
        {
            do
            {
                var message = await inbox.ReceiveAsync(timeoutPerMessage).ConfigureAwait(false);
                CheckExecutionError(message);
                if (IsExpected(message))
                    _allExpectedMessages.Add(message);
            }
            while (!IsAllExpectedMessagedReceived());
        }

        private bool IsAllExpectedMessagedReceived()
        {
            return ConditionBuilder.StopCondition(_allExpectedMessages);
        }

        private bool IsExpected(object message)
        {
            return ConditionBuilder.MessageFilters
                                   .Where(p => p.Key.IsInstanceOfType(message))
                                   .SelectMany(v => v.Value)
                                   .Any(filter => filter(message));
        }

        private static void CheckExecutionError(object t)
        {
            t.Match()
             .With<Status.Failure>(r => ExceptionDispatchInfo.Capture(r.Cause).Throw())
             .With<Failure>(r => ExceptionDispatchInfo.Capture(r.Exception).Throw());
        }
    }
}