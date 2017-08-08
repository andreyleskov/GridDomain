using System;

namespace GridDomain.EventSourcing.Sagas
{
    public class SagaTransitionException : Exception
    {
        public SagaTransitionException(object transitionMessage, Exception inner)
            : base("Saga transition raised an error", inner)
        {
            TransitionMessage = transitionMessage;
        }

        public object TransitionMessage { get; }
    }
}