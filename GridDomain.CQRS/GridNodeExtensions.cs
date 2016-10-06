using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using GridDomain.Common;

namespace GridDomain.CQRS
{

    [Obsolete("Use GridNodeCommandingExtensions instead")]
    public static class GridNodeExtensions
    {
        public static Task<T> Execute<T>(this ICommandExecutor node, ICommand command, params ExpectedMessage[] expectedMessage)
        {
            return node.Execute(new CommandPlan<T>(command, expectedMessage));
        }

        public static Task<T> Execute<T>(this ICommandExecutor node, ICommand command, ExpectedMessage<T> expectedMessage)
        {
            return node.Execute<T>(CommandPlan.New(command, expectedMessage));
        }

        public static T Execute<T>(this ICommandExecutor node, ICommand command, TimeSpan timeout, ExpectedMessage<T> expectedMessage)
        {
            return node.Execute<T>(CommandPlan.New(command, timeout,expectedMessage)).Result;
        }
    }
}