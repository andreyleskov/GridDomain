using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using Xunit;

namespace GridDomain.Tests.Unit.MessageWaiting
{
    public class AkkaWaiter_messages_test_A_and_B : AkkaWaiterTest
    {
        class B
        {
            
        }
        private readonly MessageMetadataEnvelop<string> _messageEnvelopeA = new MessageMetadataEnvelop<string>("et", MessageMetadata.Empty);
        private readonly MessageMetadataEnvelop<B> _messageEnvelopeB = new MessageMetadataEnvelop<B>(new B(), MessageMetadata.Empty);
        private IMessagesExpectation _messagesExpectation;

        protected override Task<IWaitResult> ConfigureWaiter(MessagesWaiter waiter)
        {
            var task = Waiter.Expect<string>().And<B>().Create();
            _messagesExpectation = Waiter.CreateMessagesExpectation();
            
            Publish(_messageEnvelopeA.Message);
            Publish(_messageEnvelopeB.Message);

            return task;
        }

        [Fact]
        public async Task A_and_B_should_be_received()
        {
            await ExpectMsg(_messageEnvelopeB.Message);
            await ExpectMsg(_messageEnvelopeA.Message);
        }

        [Fact]
        public void Condition_wait_end_should_be_false_on_A()
        {
            var sampleObjectsReceived = new object[] {_messageEnvelopeA};
            Assert.False(_messagesExpectation.IsExpectationFulfilled(sampleObjectsReceived));
        }

        [Fact]
        public void Condition_wait_end_should_be_false_on_B()
        {
            var sampleObjectsReceived = new object[] {_messageEnvelopeB};
            Assert.False(_messagesExpectation.IsExpectationFulfilled(sampleObjectsReceived));
        }

        [Fact]
        public void Condition_wait_end_should_be_true_on_A_and_B()
        {
            var sampleObjectsReceived = new object[] {_messageEnvelopeA, _messageEnvelopeB};
            Assert.True(_messagesExpectation.IsExpectationFulfilled(sampleObjectsReceived));
        }
    }
}