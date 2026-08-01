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
                new RecoverCardEffectHandler()
            };
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
