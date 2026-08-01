using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Application.Intents;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Application
{
    public sealed class CombatTurnLifecycleCoordinator :
        ITimelineActionProcessor,
        ITimelineClearingProcessor,
        IEnemyIntentRefresher
    {
        private readonly CombatSliceState _state;
        private readonly TimelineGrid _timeline;
        private readonly IEnemyIntentSourceCatalog _intentSources;
        private readonly EnemyIntentApplicationService _intentService;
        private readonly TowerBuildingBehaviorProcessor _towerProcessor;
        private readonly PoisonTurnStartProcessor _poisonProcessor;
        private readonly TurnLifecycleRunner _runner;
        private readonly Dictionary<TimelineActionIdentity, EnemyIntentScheduledAction>
            _scheduledIntents =
                new Dictionary<TimelineActionIdentity, EnemyIntentScheduledAction>();
        private readonly Dictionary<TimelineActionIdentity, TimelineActionPresentationSnapshot>
            _scheduledIntentSnapshots =
                new Dictionary<TimelineActionIdentity, TimelineActionPresentationSnapshot>();
        private readonly List<EnemyIntentResolveResult> _intentResolveResults =
            new List<EnemyIntentResolveResult>();
        private IReadOnlyList<LifecycleOccupantChangeResult> _lifecycleChanges =
            Array.Empty<LifecycleOccupantChangeResult>();
        private TimelineResolutionBatch _resolutionBatch;
        private bool _initialStartCompleted;

        public CombatTurnLifecycleCoordinator(
            CombatSliceState state,
            TimelineGrid timeline,
            IEnemyIntentSourceCatalog intentSources,
            EnemyIntentApplicationService intentService = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            _intentSources = intentSources ?? throw new ArgumentNullException(nameof(intentSources));
            _intentService = intentService ?? new EnemyIntentApplicationService();
            _towerProcessor = new TowerBuildingBehaviorProcessor(_state);
            _poisonProcessor = new PoisonTurnStartProcessor(_state);
            _runner = new TurnLifecycleRunner(
                this,
                _towerProcessor,
                this,
                _poisonProcessor,
                enemyIntentRefresher: this);
        }

        public TurnLifecycleResult LastLifecycleResult => _runner.LastResult;

        public ResolutionSnapshot LastResolution { get; private set; }

        public IReadOnlyList<LifecycleOccupantChangeResult> LifecycleChanges =>
            _lifecycleChanges;

        public IReadOnlyList<EnemyIntentResolveResult> IntentResolveResults =>
            new ReadOnlyCollection<EnemyIntentResolveResult>(_intentResolveResults);

        public long CurrentActionSequence { get; private set; } = 1;

        public int NextActionOrdinal { get; private set; }

        public TurnLifecycleResult RunInitialStart()
        {
            if (_initialStartCompleted)
            {
                return _runner.LastResult;
            }

            ResetCycleResults();
            var result = _runner.Run(TurnLifecycleRequest.InitialStart());
            if (result.Succeeded)
            {
                _initialStartCompleted = true;
                CaptureLifecycleChanges();
            }

            return result;
        }

        public TurnLifecycleResult RunEndTurn()
        {
            if (!_initialStartCompleted)
            {
                throw new InvalidOperationException(
                    "Initial start must complete before the first end-turn lifecycle.");
            }

            ResetCycleResults();
            var plan = _timeline.CreateResolutionPlan();
            _resolutionBatch = _timeline.BeginResolution(_state);
            if (_resolutionBatch.IsComplete)
            {
                LastResolution = _resolutionBatch.Complete();
            }

            var result = _runner.Run(TurnLifecycleRequest.EndTurn(plan));
            CaptureLifecycleChanges();
            return result;
        }

        public bool TryGetScheduledIntentSnapshot(
            TimelineActionIdentity actionId,
            out TimelineActionPresentationSnapshot snapshot)
        {
            return _scheduledIntentSnapshots.TryGetValue(actionId, out snapshot);
        }

        TurnLifecycleStepResult ITimelineActionProcessor.Resolve(
            TurnLifecycleContext context,
            TimelineActionPlanEntry action)
        {
            if (_resolutionBatch == null)
            {
                return TurnLifecycleStepResult.Failed(
                    "The timeline resolution batch was not prepared.");
            }

            try
            {
                if (action.ActorKind == TimelineActorKind.Enemy)
                {
                    if (!_scheduledIntents.TryGetValue(action.ActionId, out var scheduled))
                    {
                        return TurnLifecycleStepResult.Failed(
                            "The enemy action identity has no scheduled intent snapshot.");
                    }

                    var result = _intentService.Resolve(scheduled, CaptureWorld());
                    _intentResolveResults.Add(result);
                    _resolutionBatch.Resolve(action.ActionId, result.Succeeded);
                }
                else
                {
                    _resolutionBatch.Resolve(action.ActionId);
                }

                if (_resolutionBatch.IsComplete)
                {
                    LastResolution = _resolutionBatch.Complete();
                }

                return TurnLifecycleStepResult.Successful;
            }
            catch (Exception exception)
            {
                return TurnLifecycleStepResult.Failed(exception.Message);
            }
        }

        TurnLifecycleStepResult ITimelineClearingProcessor.Clear(TurnLifecycleContext context)
        {
            if (_resolutionBatch == null || !_resolutionBatch.IsComplete)
            {
                return TurnLifecycleStepResult.Failed(
                    "Timeline clearing requires a completed resolution batch.");
            }

            _timeline.ClearScheduledActions();
            _scheduledIntents.Clear();
            _scheduledIntentSnapshots.Clear();
            _resolutionBatch = null;
            return TurnLifecycleStepResult.Successful;
        }

        TurnLifecycleStepResult IEnemyIntentRefresher.Refresh(TurnLifecycleContext context)
        {
            var sources = _intentSources.CaptureSources(_state);
            if (sources == null)
            {
                return TurnLifecycleStepResult.Failed(
                    "The enemy intent source catalog returned no snapshot.");
            }

            var generation = _intentService.Generate(
                new EnemyIntentWorldSnapshot(
                    _state.Board.Coordinates,
                    sources,
                    _state.CaptureOccupants()),
                context.Sequence,
                0,
                _state.Seed,
                CaptureOccupiedCells(),
                _timeline.Width,
                _timeline.Height);

            _scheduledIntents.Clear();
            _scheduledIntentSnapshots.Clear();
            for (var index = 0; index < generation.Scheduled.Count; index++)
            {
                var scheduled = generation.Scheduled[index];
                if (!_timeline.TryPlace(scheduled.Action))
                {
                    return TurnLifecycleStepResult.Failed(
                        "A generated enemy intent could not be placed on the shared timeline.");
                }

                _scheduledIntents.Add(scheduled.Action.ActionId, scheduled);
                _scheduledIntentSnapshots.Add(
                    scheduled.Action.ActionId,
                    _intentService.CreateScheduledSnapshot(scheduled));
            }

            CurrentActionSequence = context.Sequence;
            NextActionOrdinal = sources.Count;
            return TurnLifecycleStepResult.Successful;
        }

        private EnemyIntentWorldSnapshot CaptureWorld()
        {
            return new EnemyIntentWorldSnapshot(
                _state.Board.Coordinates,
                _intentSources.CaptureSources(_state),
                _state.CaptureOccupants());
        }

        private IReadOnlyList<TimelineCell> CaptureOccupiedCells()
        {
            var cells = new List<TimelineCell>();
            var actions = _timeline.ScheduledActions;
            for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                var action = actions[actionIndex];
                for (var shapeIndex = 0; shapeIndex < action.Shape.Count; shapeIndex++)
                {
                    cells.Add(action.Origin + action.Shape[shapeIndex]);
                }
            }

            return cells;
        }

        private void ResetCycleResults()
        {
            LastResolution = null;
            _intentResolveResults.Clear();
            _lifecycleChanges = Array.Empty<LifecycleOccupantChangeResult>();
        }

        private void CaptureLifecycleChanges()
        {
            var changes = new List<LifecycleOccupantChangeResult>();
            if (_towerProcessor.LastResult != null)
            {
                changes.AddRange(_towerProcessor.LastResult.Changes);
            }

            if (_poisonProcessor.LastResult != null)
            {
                changes.AddRange(_poisonProcessor.LastResult.OccupantChanges);
            }

            _lifecycleChanges = new ReadOnlyCollection<LifecycleOccupantChangeResult>(changes);
        }
    }
}
