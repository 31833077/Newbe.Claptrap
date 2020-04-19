using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Microsoft.Extensions.Logging;
using Newbe.Claptrap.Core;

namespace Newbe.Claptrap.Autofac
{
    public class InitialStateDataFactory : IInitialStateDataFactory
    {
        private readonly IIndex<string, IInitialStateDataFactoryHandler> _handlers;
        private readonly ILogger<InitialStateDataFactory> _logger;

        public InitialStateDataFactory(
            IIndex<string, IInitialStateDataFactoryHandler> handlers,
            ILogger<InitialStateDataFactory> logger)
        {
            _handlers = handlers;
            _logger = logger;
        }

        public Task<IStateData> Create(IActorIdentity identity)
        {
            var handler = _handlers[identity.TypeCode];
            _logger.LogInformation("custom handler for creating state data found {actorTypeCode} {handler}",
                identity.TypeCode,
                handler);
            return handler.Create(identity);
        }
    }
}