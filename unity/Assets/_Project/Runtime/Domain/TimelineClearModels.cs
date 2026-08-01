using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum TimelineClearCellState
    {
        OutOfBounds,
        Empty,
        Occupied
    }

    public sealed class TimelineClearActionSnapshot
    {
        internal TimelineClearActionSnapshot(TimelineAction action)
        {
            Origin = action.Origin;
            ActorKind = action.ActorKind;
            CardId = action.CardId;

            var shape = new List<TimelineCell>(action.Shape.Count);
            var occupiedCells = new List<TimelineCell>(action.Shape.Count);
            for (var index = 0; index < action.Shape.Count; index++)
            {
                shape.Add(action.Shape[index]);
                occupiedCells.Add(action.Origin + action.Shape[index]);
            }

            Shape = new ReadOnlyCollection<TimelineCell>(shape);
            OccupiedCells = new ReadOnlyCollection<TimelineCell>(occupiedCells);
        }

        public TimelineCell Origin { get; }

        public TimelineActorKind ActorKind { get; }

        public string CardId { get; }

        public IReadOnlyList<TimelineCell> Shape { get; }

        public IReadOnlyList<TimelineCell> OccupiedCells { get; }
    }

    public sealed class TimelineClearCellPreview
    {
        internal TimelineClearCellPreview(
            TimelineCell coordinate,
            TimelineClearCellState state,
            TimelineClearActionSnapshot hitAction)
        {
            Coordinate = coordinate;
            State = state;
            HitAction = hitAction;
        }

        public TimelineCell Coordinate { get; }

        public TimelineClearCellState State { get; }

        public TimelineClearActionSnapshot HitAction { get; }
    }

    public sealed class TimelineClearPreview
    {
        internal TimelineClearPreview(
            TimelineCell origin,
            bool isInBounds,
            IReadOnlyList<TimelineClearCellPreview> cells,
            IReadOnlyList<TimelineClearActionSnapshot> hitActions)
        {
            Origin = origin;
            IsInBounds = isInBounds;
            Cells = Copy(cells);
            HitActions = Copy(hitActions);
        }

        public TimelineCell Origin { get; }

        public bool IsInBounds { get; }

        public IReadOnlyList<TimelineClearCellPreview> Cells { get; }

        public IReadOnlyList<TimelineClearActionSnapshot> HitActions { get; }

        private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> source)
        {
            var result = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return new ReadOnlyCollection<T>(result);
        }
    }

    public sealed class TimelineClearResult
    {
        internal TimelineClearResult(
            bool succeeded,
            TimelineClearPreview preview,
            IReadOnlyList<TimelineClearActionSnapshot> removedActions,
            int removedCellCount)
        {
            Succeeded = succeeded;
            Preview = preview;
            RemovedActions = Copy(removedActions);
            RemovedCellCount = removedCellCount;
        }

        public bool Succeeded { get; }

        public TimelineClearPreview Preview { get; }

        public IReadOnlyList<TimelineClearActionSnapshot> RemovedActions { get; }

        public int RemovedCellCount { get; }

        private static ReadOnlyCollection<TimelineClearActionSnapshot> Copy(
            IReadOnlyList<TimelineClearActionSnapshot> source)
        {
            var result = new List<TimelineClearActionSnapshot>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return new ReadOnlyCollection<TimelineClearActionSnapshot>(result);
        }
    }
}
