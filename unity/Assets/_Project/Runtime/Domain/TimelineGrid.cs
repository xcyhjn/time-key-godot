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

        public TimelineGrid(int width = DefaultWidth, int height = DefaultHeight)
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
        }

        public int Width { get; }

        public int Height { get; }

        public int OccupiedCellCount => _cells.Count;

        public bool CanPlace(TimelineAction action)
        {
            return IsPlacementValid(action);
        }

        public bool TryPlace(TimelineAction action)
        {
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

            for (var index = 0; index < orderedActions.Count; index++)
            {
                var action = orderedActions[index];
                var kind = action.ActorKind == TimelineActorKind.Player ? "player" : "enemy";
                var record = new TimelineSnapshotAction(action.Origin, kind, action.CardId);
                resolutionOrder.Add(record);

                if (action.ActorKind == TimelineActorKind.Player)
                {
                    timeline.Add(record);
                    state.ApplyDamage(action.TargetId, action.Damage);
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
                resolutionOrder);
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
