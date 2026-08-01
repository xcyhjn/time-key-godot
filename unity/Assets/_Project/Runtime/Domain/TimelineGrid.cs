using System;
using System.Collections.Generic;

namespace TimeKey.Domain
{
    public sealed class TimelineGrid
    {
        public const int DefaultWidth = 12;
        public const int DefaultHeight = 3;

        private readonly Dictionary<TimelineCell, TimelineAction> _cells =
            new Dictionary<TimelineCell, TimelineAction>();
        private readonly HashSet<TimelineAction> _placedActions =
            new HashSet<TimelineAction>();
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

            _placedActions.Add(action);
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
                        ReferenceEquals(occupyingAction, action))
                    {
                        _cells.Remove(cell);
                        removedCellCount++;
                    }
                }

                _placedActions.Remove(action);
            }

            return new TimelineClearResult(true, preview, removedActions, removedCellCount);
        }

        private bool IsPlacementValid(TimelineAction action)
        {
            if (action == null || _placedActions.Contains(action))
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
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var orderedActions = GetActionsInResolutionOrder();
            var timeline = new List<TimelineSnapshotAction>();
            var resolutionOrder = new List<TimelineSnapshotAction>(orderedActions.Count);
            var targetHpBefore = state.TargetHp;
            var enemyIntentResolved = false;
            var effectResults = new CardEffectResultBuffer();

            for (var index = 0; index < orderedActions.Count; index++)
            {
                var action = orderedActions[index];
                var kind = action.ActorKind == TimelineActorKind.Player ? "player" : "enemy";
                var record = new TimelineSnapshotAction(action.Origin, kind, action.CardId);
                resolutionOrder.Add(record);

                if (action.ActorKind == TimelineActorKind.Player)
                {
                    timeline.Add(record);
                    ApplyPlayerEffects(state, action, effectResults);
                }
                else
                {
                    enemyIntentResolved = true;
                }
            }

            Clear();

            return new ResolutionSnapshot(
                state.Turn,
                timeline,
                targetHpBefore,
                state.TargetHp,
                enemyIntentResolved,
                state.Seed,
                resolutionOrder,
                effectResults.TileResults,
                effectResults.OccupantResults);
        }

        private void ApplyPlayerEffects(
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
            var hitActionSet = new HashSet<TimelineAction>();
            var snapshots = new Dictionary<TimelineAction, TimelineClearActionSnapshot>();
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

                if (!snapshots.TryGetValue(action, out var snapshot))
                {
                    snapshot = new TimelineClearActionSnapshot(action);
                    snapshots.Add(action, snapshot);
                }

                if (hitActionSet.Add(action))
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
                hitActionSnapshots.Add(snapshots[hitActions[index]]);
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
            var processed = new HashSet<TimelineAction>();

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    TimelineAction action;
                    if (_cells.TryGetValue(new TimelineCell(x, y), out action) && processed.Add(action))
                    {
                        result.Add(action);
                    }
                }
            }

            return result;
        }

        private void Clear()
        {
            _cells.Clear();
            _placedActions.Clear();
        }
    }
}
