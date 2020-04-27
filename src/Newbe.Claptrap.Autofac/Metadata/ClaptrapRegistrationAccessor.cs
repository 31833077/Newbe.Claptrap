using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.Metadata;

namespace Newbe.Claptrap.Autofac
{
    public class ClaptrapRegistrationAccessor : IClaptrapRegistrationAccessor
    {
        private readonly ClaptrapRegistration _claptrapRegistration;
        private readonly ILogger<ClaptrapRegistrationAccessor> _logger;
        private readonly ILookup<string, EventTypeHandlerRegistration> _actorTypeCodeLookup;
        private readonly Dictionary<string, ActorTypeRegistration> _codeBaseActorStateType;
        private readonly Dictionary<Type, ActorTypeRegistration> _typeBaseActorStateType;

        public ClaptrapRegistrationAccessor(
            ILogger<ClaptrapRegistrationAccessor> logger,
            ClaptrapRegistration claptrapRegistration)
        {
            _logger = logger;
            _claptrapRegistration = claptrapRegistration;
            _actorTypeCodeLookup = _claptrapRegistration.EventHandlerTypeRegistrations.ToLookup(x => x.ActorTypeCode);
            _codeBaseActorStateType =
                claptrapRegistration.ActorTypeRegistrations.ToDictionary(x => x.ActorTypeCode);
            _typeBaseActorStateType =
                claptrapRegistration.ActorTypeRegistrations.ToDictionary(x => x.ActorStateDataType);
        }

        public Type FindEventDataType(string actorTypeCode, string eventTypeCode)
        {
            var re = _actorTypeCodeLookup[actorTypeCode].Single(x => x.EventTypeCode == eventTypeCode);
            return re.EventDataType;
        }

        public Type? FindEventHandlerType(string actorTypeCode, string eventTypeCode)
        {
            var eventTypeHandlerRegistration =
                _actorTypeCodeLookup[actorTypeCode].Single(x => x.EventTypeCode == eventTypeCode);
            return eventTypeHandlerRegistration.EventHandlerType;
        }


        public Type FindStateDataType(string actorTypeCode)
        {
            if (_codeBaseActorStateType.TryGetValue(actorTypeCode, out var re))
            {
                _logger.LogDebug(
                    "state data type found for {actorTypeCode} {stateDataType}",
                    actorTypeCode,
                    re);
                return re.ActorStateDataType;
            }

            var noneStateDataType = typeof(NoneStateData);
            _logger.LogDebug("state data type not found for {actorTypeCode}, {noneStateDataType} will be used",
                actorTypeCode, noneStateDataType);
            return noneStateDataType;
        }

        public string FindActorTypeCode(Type type)
        {
            if (_typeBaseActorStateType.TryGetValue(type, out var re))
            {
                _logger.LogDebug(
                    "state data type found for {actorTypeCode} {stateDataType}",
                    type,
                    re);
                return re.ActorTypeCode;
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}