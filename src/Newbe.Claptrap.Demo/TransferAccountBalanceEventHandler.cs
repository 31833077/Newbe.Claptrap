using System.Threading.Tasks;
using Newbe.Claptrap.Context;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.Demo.Models;
using Newbe.Claptrap.EventHandler;
using Newbe.Claptrap.Orleans;

namespace Newbe.Claptrap.Demo
{
    [ClaptrapEventHandler(typeof(AccountStateData), typeof(AccountBalanceChangeEventData))]
    public class TransferAccountBalanceEventHandler : IEventHandler
    {
        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public Task<IState> HandleEvent(IEventContext eventContext)
        {
            var eventContextEvent = (AccountBalanceChangeEventData) eventContext.Event.Data;
            var accountStateData = (AccountStateData) eventContext.State.Data;
            accountStateData.Balance += eventContextEvent.Diff;
            return Task.FromResult(eventContext.State);
        }
    }
}