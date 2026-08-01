using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public sealed class TimelineGrid
    {
        public const int DefaultWidth = 12;
        public const int DefaultHeight = 3;

        private readonly Dictionary<TimelineCell, TimelineAction> _cells =
            new Dictionary<TimelineCell, TimelineAction>();
        private readonly Dictionary<TimelineActionIdentity, TimelineAction> _placedActions =
            new Dictionary<TimelineActionIdentity, TimelineAction>();
        private readonly Dictionary<CardEffectKind, ICardEffectHandler> _effectHandlers =
            new Dictionary<CardEffectKind, ICardEffectHandler>();

        public TimelineGrid(
            int width = DefaultWidth,
            int height = DefaultHeight,
            IReadOnlyList<ICardEffectHandler> effectHandlers = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            RegisterEffectHandlers(effectHandlers ?? CreateDefaultEffectHandlers());
        }

        public int Width { get; }

        public int Height { get; }

        public int OccupiedCellCount => _cells.Count;

        public IReadOnlyList<TimelineAction> ScheduledActions =>
            new ReadOnlyCollection<TimelineAction>(GetActionsInResolutionOrder());

        public bool CanPlace(TimelineAction action)
        {
            EnsureEffectsSupported(action);
            return IsPlacementValid(action);
        }

        public bool TryPlace(TimelineAction action)
        {
            EnsureEffectsSupported(action);
            if (!IsPlacementValid(action))
            {
                return false;
            }

            for (var index = 0; index < action.Shape.Count; index++)
            {
                _cells.Add(action.Origin + action.Shape[index], action);
            }

            _placedActions.Add(action.ActionId, action);
            return true;
        }

        public TimelineClearPreview PreviewClear(TimelineCell origin, CardEffect clearEffect)
        {
            EnsureClearEffect(clearEffect);
            return BuildClearPreview(origin, clearEffect, out _);
        }

        public TimelineClearResult TryClear(TimelineCell origin, CardEffect clearEffect)
        {
            EnsureClearEffect(clearEffect);
            var preview = BuildClearPreview(origin, clearEffect, out var hitActions);
            if (!preview.IsInBounds)
            {
                return new TimelineClearResult(
                    false,
                    preview,
                    Array.Empty<TimelineClearActionSnapshot>(),
                    0);
            }

            var removedActions = new List<TimelineClearActionSnapshot>(hitActions.Count);
            var removedCellCount = 0;
            for (var actionIndex = 0; actionIndex < hitActions.Count; actionIndex++)
            {
                var action = hitActions[actionIndex];
                removedActions.Add(new TimelineClearActionSnapshot(action));
                for (var shapeIndex = 0; shapeIndex < action.Shape.Count; shapeIndex++)
                {
                    var cell = action.Origin + action.Shape[shapeIndex];
                    if (_cells.TryGetValue(cell, out var occupyingAction) &&
                        occupyingAction.ActionId == action.ActionId)
                    {
                        _cells.Remove(cell);
                        removedCellCount++;
                    }
                }

                _placedActions.Remove(action.ActionId);
            }

            return new TimelineClearResult(true, preview, removedActions, removedCellCount);
        }

        private bool IsPlacementValid(TimelineAction action)
        {
            if (action == null || _placedActions.ContainsKey(action.ActionId))
            {
                return false;
            }

            for (var index = 0; index < action.Shape.Count; index++)
            {
                var cell = action.Origin + action.Shape[index];
                if (!Contains(cell) || _cells.ContainsKey(cell))
                {
                    return false;
                }
            }

            return true;
        }

        public ResolutionSnapshot Resolve(CombatSliceState state)
        {
            var batch = BeginResolution(state);
            while (!batch.IsComplete)
            {
                batch.ResolveNext();
            }

            var result = batch.Complete();
            ClearScheduledActions();
            return result;
        }

        public TimelineResolutionBatch BeginResolution(CombatSliceState state)
        {
            return new TimelineResolutionBatch(
                this,
                state ?? throw new ArgumentNullException(nameof(state)),
                GetActionsInResolutionOrder());
        }

        public bool RemoveAction(TimelineActionIdentity actionId)
        {
            if (!_placedActions.TryGetValue(actionId, out var action))
            {
                return false;
            }

            for (var index = 0; index < action.Shape.Count; index++)
            {
                _cells.Remove(action.Origin + action.Shape[index]);
            }

            _placedActions.Remove(actionId);
            return true;
        }

        public void ClearScheduledActions()
        {
            Clear();
        }

        internal void ApplyPlayerEffects(
            CombatSliceState state,
            TimelineAction action,
            CardEffectResultBuffer effectResults)
        {
            for (var effectIndex = 0; effectIndex < action.Effects.Count; effectIndex++)
            {
                var effect = action.Effects[effectIndex];
                _effectHandlers[effect.Kind].Apply(state, action, effect, effectResults);
            }
        }

        private void RegisterEffectHandlers(IReadOnlyList<ICardEffectHandler> handlers)
        {
            for (var index = 0; index < handlers.Count; index++)
            {
                var handler = handlers[index] ??
                    throw new ArgumentException("Card effect handlers cannot contain null entries.", nameof(handlers));
                if (_effectHandlers.ContainsKey(handler.Kind))
                {
                    throw new ArgumentException("Duplicate card effect handler: " + handler.Kind + ".", nameof(handlers));
                }

                _effectHandlers.Add(handler.Kind, handler);
            }
        }

        private void EnsureEffectsSupported(TimelineAction action)
        {
            if (action == null)
            {
                return;
            }

            for (var index = 0; index < action.Effects.Count; index++)
            {
                var effect = action.Effects[index];
                if (!_effectHandlers.TryGetValue(effect.Kind, out var handler))
                {
                    throw new UnsupportedCardEffectException(effect.Kind);
                }

                if (!handler.Supports(effect))
                {
                    throw new UnsupportedCardEffectException(effect);
                }
            }
        }

        private static IReadOnlyList<ICardEffectHandler> CreateDefaultEffectHandlers()
        {
            return new ICardEffectHandler[]
            {
                new DamageCardEffectHandler(),
                new ElevationCardEffectHandler(),
                new RecoverCardEffectHandler(),
                new BuiltCardEffectHandler(),
                new PoisonCardEffectHandler()
            };
        }

        private TimelineClearPreview BuildClearPreview(
            TimelineCell origin,
            CardEffect clearEffect,
            out List<TimelineAction> hitActions)
        {
            var cells = new List<TimelineClearCellPreview>(clearEffect.ClearMask.Count);
            hitActions = new List<TimelineAction>();
            var hitActionSet = new HashSet<TimelineActionIdentity>();
            var snapshots =
                new Dictionary<TimelineActionIdentity, TimelineClearActionSnapshot>();
            var isInBounds = true;

            for (var index = 0; index < clearEffect.ClearMask.Count; index++)
            {
                var cell = origin + clearEffect.ClearMask[index];
                if (!Contains(cell))
                {
                    isInBounds = false;
                    cells.Add(new TimelineClearCellPreview(
                        cell,
                        TimelineClearCellState.OutOfBounds,
                        null));
                    continue;
                }

                if (!_cells.TryGetValue(cell, out var action))
                {
                    cells.Add(new TimelineClearCellPreview(
                        cell,
                        TimelineClearCellState.Empty,
                        null));
                    continue;
                }

                if (!snapshots.TryGetValue(action.ActionId, out var snapshot))
                {
                    snapshot = new TimelineClearActionSnapshot(action);
                    snapshots.Add(action.ActionId, snapshot);
                }

                if (hitActionSet.Add(action.ActionId))
                {
                    hitActions.Add(action);
                }

                cells.Add(new TimelineClearCellPreview(
                    cell,
                    TimelineClearCellState.Occupied,
                    snapshot));
            }

            var hitActionSnapshots = new List<TimelineClearActionSnapshot>(hitActions.Count);
            for (var index = 0; index < hitActions.Count; index++)
            {
                hitActionSnapshots.Add(snapshots[hitActions[index].ActionId]);
            }

            return new TimelineClearPreview(origin, isInBounds, cells, hitActionSnapshots);
        }

        private static void EnsureClearEffect(CardEffect clearEffect)
        {
            if (clearEffect.Kind != CardEffectKind.Clear || clearEffect.ClearMask.Count == 0)
            {
                throw new ArgumentException(
                    "Timeline clear requires a typed Clear effect with a non-empty mask.",
                    nameof(clearEffect));
            }
        }

        private bool Contains(TimelineCell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }

        private List<TimelineAction> GetActionsInResolutionOrder()
        {
            var result = new List<TimelineAction>(_placedActions.Count);
            var processed = new HashSet<TimelineActionIdentity>();

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    TimelineAction action;
                    if (_cells.TryGetValue(new TimelineCell(x, y), out action) &&
                        processed.Add(action.ActionId))
                    {
                        result.Add(action);
                    }
                }
            }

            return result;
        }

        public TimelineActionPlan CreateResolutionPlan()
        {
            var actions = GetActionsInResolutionOrder();
            var entries = new List<TimelineActionPlanEntry>(actions.Count);
            for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                var action = actions[actionIndex];
                var occupiedCells = new List<TimelineCell>(action.Shape.Count);
                for (var shapeIndex = 0; shapeIndex < action.Shape.Count; shapeIndex++)
                {
                    occupiedCells.Add(action.Origin + action.Shape[shapeIndex]);
                }

                entries.Add(new TimelineActionPlanEntry(
                    action.ActionId,
                    action.ActorKind,
                    action.CardId,
                    action.Origin,
                    occupiedCells));
            }

            return new TimelineActionPlan(entries, Width, Height);
        }

        private void Clear()
        {
            _cells.Clear();
            _placedActions.Clear();
        }
    }

    public sealed class TimelineResolutionBatch
    {
        private readonly TimelineGrid _grid;
        private readonly CombatSliceState _state;
        private readonly IReadOnlyList<TimelineAction> _actions;
        private readonly List<TimelineSnapshotAction> _playerTimeline =
            new List<TimelineSnapshotAction>();
        private readonly List<TimelineSnapshotAction> _resolutionOrder =
            new List<TimelineSnapshotAction>();
        private readonly CardEffectResultBuffer _effectResults = new CardEffectResultBuffer();
        private readonly int _targetHpBefore;
        private int _nextIndex;
        private bool _enemyIntentResolved;
        private ResolutionSnapshot _snapshot;

        internal TimelineResolutionBatch(
            TimelineGrid grid,
            CombatSliceState state,
            IReadOnlyList<TimelineAction> actions)
        {
            _grid = grid;
            _state = state;
            _actions = actions;
            _targetHpBefore = state.TargetHp;
        }

        public bool IsComplete => _nextIndex == _actions.Count;

        public int ResolvedActionCount => _nextIndex;

        public void ResolveNext(bool enemyIntentResolved = true)
        {
            if (IsComplete)
            {
                throw new InvalidOperationException("Every timeline action is already resolved.");
            }

            Resolve(_actions[_nextIndex].ActionId, enemyIntentResolved);
        }

        public void Resolve(
            TimelineActionIdentity actionId,
            bool enemyIntentResolved = true)
        {
            if (IsComplete || _actions[_nextIndex].ActionId != actionId)
            {
                throw new InvalidOperationException(
                    "Timeline actions must resolve exactly once in the frozen grid order.");
            }

            var action = _actions[_nextIndex++];
            var record = new TimelineSnapshotAction(
                action.ActionId,
                action.Origin,
                action.ActorKind == TimelineActorKind.Player ? "player" : "enemy",
                action.CardId);
            _resolutionOrder.Add(record);
            if (action.ActorKind == TimelineActorKind.Player)
            {
                _playerTimeline.Add(record);
                _grid.ApplyPlayerEffects(_state, action, _effectResults);
            }
            else if (enemyIntentResolved)
            {
                _enemyIntentResolved = true;
            }
        }

        public ResolutionSnapshot Complete()
        {
            if (!IsComplete)
            {
                throw new InvalidOperationException(
                    "The resolution snapshot cannot complete before every action resolves.");
            }

            if (_snapshot == null)
            {
                _snapshot = new ResolutionSnapshot(
                    _state.Turn,
                    _playerTimeline,
                    _targetHpBefore,
                    _state.TargetHp,
                    _enemyIntentResolved,
                    _state.Seed,
                    _resolutionOrder,
                    _effectResults.TileResults,
                    _effectResults.OccupantResults);
            }

            return _snapshot;
        }
    }
}
