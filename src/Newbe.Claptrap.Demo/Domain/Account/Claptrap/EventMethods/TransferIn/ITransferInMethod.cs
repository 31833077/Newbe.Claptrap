using System.Threading.Tasks;
using Newbe.Claptrap.Demo.Models;
using Newbe.Claptrap.Demo.Models.EventData;

namespace Newbe.Claptrap.Demo.Domain.Account.Claptrap.EventMethods.TransferIn
{
    public interface ITransferInMethod
    {
        Task<EventMethodResult<BalanceChangeEventData>> Invoke(AccountStateData stateData, decimal amount, string uid);
    }
}