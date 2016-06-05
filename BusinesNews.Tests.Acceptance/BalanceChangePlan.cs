using System;
using System.Collections.Generic;
using BusinessNews.Domain.AccountAggregate.Commands;
using NMoneys;

namespace BusinesNews.Tests.Acceptance
{
    public class BalanceChangePlan
    {
        public IReadOnlyCollection<ChangeAccountCommand> BalanceChangeCommands;
        public CreateAccountCommand AccountCreateCommand;
        public Guid AccountId;
        public Guid businessId;
        public Money TotalAmountChange;
        public Money TotalReplenish;
        public Money TotalWithdrwal;
    }
}