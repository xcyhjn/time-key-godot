using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public sealed class TileEffectResult
    {
        public TileEffectResult(
            HexCoord coordinate,
            int beforeLayers,
            int afterLayers,
            bool removed)
        {
            Coordinate = coordinate;
            BeforeLayers = beforeLayers;
            AfterLayers = afterLayers;
            Removed = removed;
        }

        public HexCoord Coordinate { get; }

        public int BeforeLayers { get; }

        public int AfterLayers { get; }

        public bool Removed { get; }
    }

    public sealed class TimelineSnapshotAction
    {
        public TimelineSnapshotAction(TimelineCell origin, string kind, string cardId)
        {
            Origin = origin;
            Kind = kind;
            CardId = cardId;
        }

        public TimelineCell Origin { get; }

        public string Kind { get; }

        public string CardId { get; }
    }

    public sealed class ResolutionSnapshot
    {
        internal ResolutionSnapshot(
            int turn,
            IReadOnlyList<TimelineSnapshotAction> timeline,
            int targetHpBefore,
            int targetHpAfter,
            bool enemyIntentResolved,
            int seed,
            IReadOnlyList<TimelineSnapshotAction> resolutionOrder,
            IReadOnlyList<TileEffectResult> effectResults = null)
        {
            Turn = turn;
            Phase = "resolved";
            Timeline = Copy(timeline);
            TargetHpBefore = targetHpBefore;
            TargetHpAfter = targetHpAfter;
            EnemyIntentResolved = enemyIntentResolved;
            Seed = seed;
            ResolutionOrder = Copy(resolutionOrder);
            EffectResults = Copy(effectResults ?? new TileEffectResult[0]);
        }

        public int Turn { get; }

        public string Phase { get; }

        public IReadOnlyList<TimelineSnapshotAction> Timeline { get; }

        public int TargetHpBefore { get; }

        public int TargetHpAfter { get; }

        public bool EnemyIntentResolved { get; }

        public int Seed { get; }

        public IReadOnlyList<TimelineSnapshotAction> ResolutionOrder { get; }

        public IReadOnlyList<TileEffectResult> EffectResults { get; }

        private static ReadOnlyCollection<TimelineSnapshotAction> Copy(
            IReadOnlyList<TimelineSnapshotAction> source)
        {
            var result = new List<TimelineSnapshotAction>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return new ReadOnlyCollection<TimelineSnapshotAction>(result);
        }

        private static ReadOnlyCollection<TileEffectResult> Copy(
            IReadOnlyList<TileEffectResult> source)
        {
            var result = new List<TileEffectResult>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return new ReadOnlyCollection<TileEffectResult>(result);
        }
    }
}
