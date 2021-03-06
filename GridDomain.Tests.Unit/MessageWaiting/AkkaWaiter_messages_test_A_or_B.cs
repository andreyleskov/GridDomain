using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using Xunit;

namespace GridDomain.Tests.Unit.MessageWaiting
{
    public class AkkaWaiter_messages_test_A_or_B : AkkaWaiterTest
    {
        class A { }

        protected override Task<IWaitResult> ConfigureWaiter(MessagesWaiter waiter)
        {
            return waiter.Expect<string>().Or<A>().Create();
        }

        [Fact]
        public async Task When_publish_one_of_subscribed_message_Then_wait_is_over_And_message_received()
        {
            var msg = new A();
            Publish(msg);
            await ExpectMsg(msg);
        }

        [Fact]
        public async Task When_publish_other_of_subscribed_message_Then_wait_is_over_And_message_received()
        {
            var msg = "testMsg";
            Publish(msg);
            await ExpectMsg(msg);
        }
    }
}