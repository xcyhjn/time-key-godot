using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum TowerBehaviorFailure
    {
        None,
        InvalidSequence,
        OccupantSnapshotUnavailable,
        StoreRejectedMutation
    }

    public sealed class TowerBuildingBehaviorResult
    {
        private readonly ReadOnlyCollection<LifecycleOccupantChangeResult> _changes;

        internal TowerBuildingBehaviorResult(
            long sequence,
            TowerBehaviorFailure failure,
            string failureReason,
            bool alreadyProcessed,
            IReadOnlyList<LifecycleOccupantChangeResult> changes)
        {
            Sequence = sequence;
            Failure = failure;
            FailureReason = failureReason;
            AlreadyProcessed = alreadyProcessed;
            _changes = Copy(changes);
        }

        public bool Succeeded => Failure == TowerBehaviorFailure.None;

        public long Sequence { get; }

        public TowerBehaviorFailure Failure { get; }

        public string FailureReason { get; }

        public bool AlreadyProcessed { get; }

        public IReadOnlyList<LifecycleOccupantChangeResult> Changes => _changes;

        private static ReadOnlyCollection<LifecycleOccupantChangeResult> Copy(
            IReadOnlyList<LifecycleOccupantChangeResult> source)
        {
            var values = new List<LifecycleOccupantChangeResult>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<LifecycleOccupantChangeResult>(values);
        }
    }

    public sealed class TowerBuildingBehaviorProcessor : IBuildingBehaviorProcessor
    {
        public const int DecayDamage = 50;

        private readonly ILifecycleOccupantStore _store;
        private readonly HashSet<long> _completedSequences = new HashSet<long>();

        public TowerBuildingBehaviorProcessor(ILifecycleOccupantStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public TowerBuildingBehaviorResult LastResult { get; private set; }

        public TurnLifecycleStepResult Run(TurnLifecycleContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = Process(context.Sequence);
            return result.Succeeded
                ? TurnLifecycleStepResult.Successful
                : TurnLifecycleStepResult.Failed(result.FailureReason);
        }

        public TowerBuildingBehaviorResult Process(long sequence)
        {
            if (sequence <= 0)
            {
                return Complete(
                    sequence,
                    TowerBehaviorFailure.InvalidSequence,
                    "A positive lifecycle sequence is required.",
                    false,
                    Array.Empty<LifecycleOccupantChangeResult>());
            }

            if (_completedSequences.Contains(sequence))
            {
                return Complete(
                    sequence,
                    TowerBehaviorFailure.None,
                    null,
                    true,
                    Array.Empty<LifecycleOccupantChangeResult>());
            }

            var snapshot = _store.CaptureOccupants();
            if (snapshot == null)
            {
                return Complete(
                    sequence,
                    TowerBehaviorFailure.OccupantSnapshotUnavailable,
                    "The lifecycle occupant snapshot is unavailable.",
                    false,
                    Array.Empty<LifecycleOccupantChangeResult>());
            }

            var towers = new List<CombatOccupantSnapshot>();
            for (var index = 0; index < snapshot.Count; index++)
            {
                var occupant = snapshot[index];
                if (DefaultLifecycleDeathPolicyResolver.IsTower(occupant) &&
                    occupant.SupportsHealth &&
                    occupant.IsAlive)
                {
                    towers.Add(occupant);
                }
            }

            towers.Sort(CompareOccupants);
            var mutations = new List<LifecycleOccupantMutation>(towers.Count);
            for (var index = 0; index < towers.Count; index++)
            {
                var tower = towers[index];
                var afterHp = Math.Max(0, tower.Hp - DecayDamage);
                var died = afterHp == 0;
                mutations.Add(new LifecycleOccupantMutation(
                    tower.RuntimeId,
                    tower.Coordinate,
                    TurnLifecyclePhase.RunningBuildingBehaviors,
                    LifecycleMutationReason.TowerDecay,
                    tower.Hp,
                    tower.PoisonStacks,
                    afterHp,
                    died ? 0 : tower.PoisonStacks,
                    remove: died,
                    deathPolicy: died ? LifecycleDeathPolicy.Remove : (LifecycleDeathPolicy?)null));
            }

            if (mutations.Count > 0 && !_store.TryApply(mutations, out var failureReason))
            {
                return Complete(
                    sequence,
                    TowerBehaviorFailure.StoreRejectedMutation,
                    string.IsNullOrWhiteSpace(failureReason)
                        ? "The lifecycle occupant store rejected Tower decay."
                        : failureReason,
                    false,
                    Array.Empty<LifecycleOccupantChangeResult>());
            }

            var changes = new List<LifecycleOccupantChangeResult>(mutations.Count);
            for (var index = 0; index < mutations.Count; index++)
            {
                changes.Add(new LifecycleOccupantChangeResult(sequence, mutations[index]));
            }

            _completedSequences.Add(sequence);
            return Complete(
                sequence,
                TowerBehaviorFailure.None,
                null,
                false,
                changes);
        }

        private TowerBuildingBehaviorResult Complete(
            long sequence,
            TowerBehaviorFailure failure,
            string failureReason,
            bool alreadyProcessed,
            IReadOnlyList<LifecycleOccupantChangeResult> changes)
        {
            LastResult = new TowerBuildingBehaviorResult(
                sequence,
                failure,
                failureReason,
                alreadyProcessed,
                changes);
            return LastResult;
        }

        private static int CompareOccupants(
            CombatOccupantSnapshot left,
            CombatOccupantSnapshot right)
        {
            var qComparison = left.Coordinate.Q.CompareTo(right.Coordinate.Q);
            if (qComparison != 0)
            {
                return qComparison;
            }

            var rComparison = left.Coordinate.R.CompareTo(right.Coordinate.R);
            return rComparison != 0
                ? rComparison
                : StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId);
        }
    }
}
