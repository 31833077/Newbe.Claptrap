using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newbe.Claptrap.Preview.Abstractions.Components;
using Newbe.Claptrap.Preview.Abstractions.Core;

namespace Newbe.Claptrap.Preview.Impl
{
    public class MinionEventHandlerFLow : IEventHandlerFLow
    {
        private readonly IStateAccessor _stateAccessor;
        private readonly IStateHolder _stateHolder;
        private readonly IEventLoader _eventLoader;
        private readonly IEventHandlerFactory _eventHandlerFactory;
        private readonly IStateRestorer _stateRestorer;
        private readonly IStateSavingFlow _stateSavingFlow;
        private readonly StateOptions _stateOptions;
        private readonly ILogger<MasterEventHandlerFLow> _logger;

        private IDisposable _eventHandleFlow = null!;
        private readonly Subject<EventItem> _incomingEventsSeq;

        private IState State
        {
            get => _stateAccessor.State;
            set => _stateAccessor.State = value;
        }

        public MinionEventHandlerFLow(
            IStateAccessor stateAccessor,
            IStateHolder stateHolder,
            IEventLoader eventLoader,
            IEventHandlerFactory eventHandlerFactory,
            IStateRestorer stateRestorer,
            IStateSavingFlow stateSavingFlow,
            StateOptions stateOptions,
            ILogger<MasterEventHandlerFLow> logger)
        {
            _stateAccessor = stateAccessor;
            _stateHolder = stateHolder;
            _eventLoader = eventLoader;
            _eventHandlerFactory = eventHandlerFactory;
            _stateRestorer = stateRestorer;
            _stateSavingFlow = stateSavingFlow;
            _stateOptions = stateOptions;
            _logger = logger;
            _incomingEventsSeq = new Subject<EventItem>();
        }

        public void Activate()
        {
            _eventHandleFlow = _incomingEventsSeq
                .SelectMany(item =>
                {
                    if (State.NextVersion == item.Event.Version)
                    {
                        return Observable.Return(item);
                    }

                    if (item.Event.Version < State.NextVersion)
                    {
                        item.TaskCompletionSource!.SetResult(0);
                        return Observable.Empty<EventItem>();
                    }


                    return LoadEventFromLoader()
                        .ToObservable()
                        .Concat(Observable.Return(item));

                    async IAsyncEnumerable<EventItem> LoadEventFromLoader()
                    {
                        // TODO config
                        const int step = 1000;
                        var versionCount = item.Event.Version - State.NextVersion;
                        var pageCount = (int) Math.Ceiling(versionCount * 1.0 / step);
                        for (var i = 0; i < pageCount; i++)
                        {
                            var left = State.NextVersion + i * step;
                            var right = Math.Min(State.NextVersion + (i + 1) * step, item.Event.Version);
                            var events = await _eventLoader.GetEventsAsync(left, right);
                            foreach (var @event in events)
                            {
                                yield return new EventItem
                                {
                                    Event = @event,
                                    TaskCompletionSource = null
                                };
                            }
                        }
                    }
                })
                .Select(item =>
                {
                    try
                    {
                        var context = new EventHandleFlowContext
                        {
                            NowState = _stateHolder.DeepCopy(State),
                            Event = item.Event,
                            TaskCompletionSource = item.TaskCompletionSource,
                        };
                        Debug.Assert(context.Event.Version == context.NowState.NextVersion);
                        context.EventContext = new EventContext(context.Event, context.NowState);
                        context.EventHandler = CreateHandler(context.EventContext);
                        return context;
                    }
                    catch (Exception e)
                    {
                        item.TaskCompletionSource?.SetException(e);
                        throw;
                    }
                })
                .Select(context => Observable.FromAsync(
                    async () =>
                    {
                        try
                        {
                            await HandleEventCoreAsync();
                            context.TaskCompletionSource?.SetResult(0);
                        }
                        catch (Exception e)
                        {
                            await HandleException(e);
                            context.TaskCompletionSource?.SetException(e);
                        }

                        async Task HandleEventCoreAsync()
                        {
                            var nextState = await context.EventHandler.HandleEvent(context.EventContext);
                            _logger.LogInformation("event handled and updating state");
                            _logger.LogDebug("start update to {@state}", nextState);
                            State = nextState;
                            State.IncreaseVersion();
                            _stateSavingFlow.OnNewStateCreated(State);
                            _logger.LogDebug("state version updated : {version}", State.Version);
                        }

                        async Task HandleException(Exception e)
                        {
                            _logger.LogWarning(e,
                                "there is an exception when handle event : {@event} . start to recover state as strategy : {strategy}",
                                context.Event,
                                _stateOptions.StateRecoveryStrategy);
                            switch (_stateOptions.StateRecoveryStrategy)
                            {
                                case StateRecoveryStrategy.FromStateHolder:
                                    State = context.NowState;
                                    break;
                                case StateRecoveryStrategy.FromStore:
                                    await _stateRestorer.RestoreAsync();
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }))
                .Concat()
                .Subscribe(_ => { },
                    ex => { _logger.LogError(ex, "thrown a exception while handling event"); });
        }

        public void Deactivate()
        {
            _eventHandleFlow?.Dispose();
            _incomingEventsSeq?.Dispose();
        }

        public Task OnNewEventReceived(IEvent @event)
        {
            var eventItem = new EventItem
            {
                Event = @event,
                TaskCompletionSource = new TaskCompletionSource<int>()
            };

            _incomingEventsSeq.OnNext(eventItem);

            var task = eventItem.TaskCompletionSource.Task;
            return task;
        }

        private class EventHandleFlowContext
        {
            public IState NowState { get; set; } = null!;
            public IEvent Event { get; set; } = null!;
            public IEventContext EventContext { get; set; } = null!;
            public IEventHandler EventHandler { get; set; } = null!;
            public TaskCompletionSource<int>? TaskCompletionSource { get; set; } = null!;
        }

        private IEventHandler CreateHandler(IEventContext eventContext)
        {
            _logger.LogTrace("creating event handler");
            var handler = _eventHandlerFactory.Create(eventContext);
            _logger.LogTrace("created event handler : {handler}", handler);
            return handler;
        }

        private struct EventItem
        {
            public IEvent Event { get; set; }
            public TaskCompletionSource<int>? TaskCompletionSource { get; set; }
        }
    }
}