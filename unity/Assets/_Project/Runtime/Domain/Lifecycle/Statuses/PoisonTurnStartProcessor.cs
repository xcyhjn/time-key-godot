using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum PoisonTurnStartFailure
    {
        None,
        InvalidSequence,
        OccupantSnapshotUnavailable,
        InvalidOccupantSnapshot,
        StackOverflow,
        StoreRejectedMutation
    }

    public sealed class PoisonSpreadResult
    {
        internal PoisonSpreadResult(
            long sequence,
            CombatOccupantSnapshot source,
            CombatOccupantSnapshot target,
            int targetAfterSpreadStacks)
        {
            Sequence = sequence;
            SourceRuntimeId = source.RuntimeId;
            SourceCoordinate = source.Coordinate;
            SourceSnapshotStacks = source.PoisonStacks;
            TargetRuntimeId = target.RuntimeId;
            TargetCoordinate = target.Coordinate;
            TargetBeforeStacks = target.PoisonStacks;
            TargetAfterSpreadStacks = targetAfterSpreadStacks;
        }

        public long Sequence { get; }

        public string SourceRuntimeId { get; }

        public HexCoord SourceCoordinate { get; }

        public int SourceSnapshotStacks { get; }

        public string TargetRuntimeId { get; }

        public HexCoord TargetCoordinate { get; }

        public int SpreadDelta => 1;

        public int TargetBeforeStacks { get; }

        public int TargetAfterSpreadStacks { get; }
    }

    public sealed class PoisonDamageResult
    {
        internal PoisonDamageResult(
            long sequence,
            CombatOccupantSnapshot source,
            long requestedDamage,
            int afterHp,
            LifecycleDeathPolicy? deathPolicy,
            bool removed)
        {
            Sequence = sequence;
            RuntimeId = source.RuntimeId;
            Coordinate = source.Coordinate;
            SnapshotPoisonStacks = source.PoisonStacks;
            BeforeHp = source.Hp;
            RequestedDamage = requestedDamage;
            AfterHp = afterHp;
            DeathPolicy = deathPolicy;
            Removed = removed;
        }

        public long Sequence { get; }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public int SnapshotPoisonStacks { get; }

        public int BeforeHp { get; }

        public long RequestedDamage { get; }

        public int AppliedDamage => BeforeHp - AfterHp;

        public int AfterHp { get; }

        public LifecycleDeathPolicy? DeathPolicy { get; }

        public bool Removed { get; }
    }

    public sealed class PoisonDecayResult
    {
        internal PoisonDecayResult(
            long sequence,
            CombatOccupantSnapshot source,
            int beforeDecayStacks,
            int afterStacks,
            bool skippedBecauseDead)
        {
            Sequence = sequence;
            RuntimeId = source.RuntimeId;
            Coordinate = source.Coordinate;
            SnapshotPoisonStacks = source.PoisonStacks;
            BeforeDecayStacks = beforeDecayStacks;
            AfterStacks = afterStacks;
            SkippedBecauseDead = skippedBecauseDead;
        }

        public long Sequence { get; }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public int SnapshotPoisonStacks { get; }

        public int BeforeDecayStacks { get; }

        public int AfterStacks { get; }

        public bool SkippedBecauseDead { get; }
    }

    public sealed class PoisonTurnStartResult
    {
        private readonly ReadOnlyCollection<PoisonSpreadResult> _spreadResults;
        private readonly ReadOnlyCollection<PoisonDamageResult> _damageResults;
        private readonly ReadOnlyCollection<PoisonDecayResult> _decayResults;
        private readonly ReadOnlyCollection<LifecycleOccupantChangeResult> _occupantChanges;

        internal PoisonTurnStartResult(
            long sequence,
            PoisonTurnStartFailure failure,
            string failureReason,
            bool alreadyProcessed,
            IReadOnlyList<PoisonSpreadResult> spreadResults,
            IReadOnlyList<PoisonDamageResult> damageResults,
            IReadOnlyList<PoisonDecayResult> decayResults,
            IReadOnlyList<LifecycleOccupantChangeResult> occupantChanges)
        {
            Sequence = sequence;
            Failure = failure;
            FailureReason = failureReason;
            AlreadyProcessed = alreadyProcessed;
            _spreadResults = Copy(spreadResults);
            _damageResults = Copy(damageResults);
            _decayResults = Copy(decayResults);
            _occupantChanges = Copy(occupantChanges);
        }

        public bool Succeeded => Failure == PoisonTurnStartFailure.None;

        public long Sequence { get; }

        public PoisonTurnStartFailure Failure { get; }

        public string FailureReason { get; }

        public bool AlreadyProcessed { get; }

        public IReadOnlyList<PoisonSpreadResult> SpreadResults => _spreadResults;

        public IReadOnlyList<PoisonDamageResult> DamageResults => _damageResults;

        public IReadOnlyList<PoisonDecayResult> DecayResults => _decayResults;

        public IReadOnlyList<LifecycleOccupantChangeResult> OccupantChanges => _occupantChanges;

        private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> source)
        {
            var values = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<T>(values);
        }
    }

    public sealed class PoisonTurnStartProcessor : ITurnStartStatusProcessor
    {
        private static readonly HexCoord[] NeighborOffsets =
        {
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1)
        };

        private readonly ILifecycleOccupantStore _store;
        private readonly ILifecycleDeathPolicyResolver _deathPolicyResolver;
        private readonly HashSet<long> _completedSequences = new HashSet<long>();

        public PoisonTurnStartProcessor(
            ILifecycleOccupantStore store,
            ILifecycleDeathPolicyResolver deathPolicyResolver = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _deathPolicyResolver = deathPolicyResolver ??
                new DefaultLifecycleDeathPolicyResolver();
        }

        public PoisonTurnStartResult LastResult { get; private set; }

        public TurnLifecycleStepResult Process(TurnLifecycleContext context)
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

        public PoisonTurnStartResult Process(long sequence)
        {
            if (sequence <= 0)
            {
                return Fail(
                    sequence,
                    PoisonTurnStartFailure.InvalidSequence,
                    "A positive lifecycle sequence is required.");
            }

            if (_completedSequences.Contains(sequence))
            {
                return Complete(
                    sequence,
                    PoisonTurnStartFailure.None,
                    null,
                    true,
                    Array.Empty<PoisonSpreadResult>(),
                    Array.Empty<PoisonDamageResult>(),
                    Array.Empty<PoisonDecayResult>(),
                    Array.Empty<LifecycleOccupantChangeResult>());
            }

            var snapshot = _store.CaptureOccupants();
            if (snapshot == null)
            {
                return Fail(
                    sequence,
                    PoisonTurnStartFailure.OccupantSnapshotUnavailable,
                    "The lifecycle occupant snapshot is unavailable.");
            }

            var ordered = new List<CombatOccupantSnapshot>(snapshot.Count);
            var byCoordinate = new Dictionary<HexCoord, CombatOccupantSnapshot>();
            var runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < snapshot.Count; index++)
            {
                var occupant = snapshot[index];
                if (occupant == null ||
                    !runtimeIds.Add(occupant.RuntimeId) ||
                    byCoordinate.ContainsKey(occupant.Coordinate))
                {
                    return Fail(
                        sequence,
                        PoisonTurnStartFailure.InvalidOccupantSnapshot,
                        "Lifecycle occupant identities and coordinates must be unique.");
                }

                ordered.Add(occupant);
                byCoordinate.Add(occupant.Coordinate, occupant);
            }

            ordered.Sort(CompareOccupants);
            var sources = new List<CombatOccupantSnapshot>();
            for (var index = 0; index < ordered.Count; index++)
            {
                var occupant = ordered[index];
                if (occupant.IsAlive &&
                    occupant.SupportsStatus &&
                    occupant.PoisonStacks > 0)
                {
                    sources.Add(occupant);
                }
            }

            var spreadPairs = new List<SpreadPair>();
            var spreadTotals = new Dictionary<string, int>(StringComparer.Ordinal);
            try
            {
                for (var sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
                {
                    var source = sources[sourceIndex];
                    for (var offsetIndex = 0; offsetIndex < NeighborOffsets.Length; offsetIndex++)
                    {
                        var targetCoordinate = Add(source.Coordinate, NeighborOffsets[offsetIndex]);
                        if (!byCoordinate.TryGetValue(targetCoordinate, out var target) ||
                            !target.IsAlive ||
                            !target.SupportsStatus)
                        {
                            continue;
                        }

                        spreadTotals.TryGetValue(target.RuntimeId, out var total);
                        spreadTotals[target.RuntimeId] = checked(total + 1);
                        spreadPairs.Add(new SpreadPair(source, target));
                    }
                }

                foreach (var target in ordered)
                {
                    if (spreadTotals.TryGetValue(target.RuntimeId, out var total))
                    {
                        checked
                        {
                            _ = target.PoisonStacks + total;
                        }
                    }
                }
            }
            catch (OverflowException)
            {
                return Fail(
                    sequence,
                    PoisonTurnStartFailure.StackOverflow,
                    "Poison stack aggregation exceeded the supported integer range.");
            }

            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < sources.Count; index++)
            {
                sourceIds.Add(sources[index].RuntimeId);
            }

            var finalStates = new Dictionary<string, FinalState>(StringComparer.Ordinal);
            var damageResults = new List<PoisonDamageResult>(sources.Count);
            var decayResults = new List<PoisonDecayResult>(sources.Count);
            for (var index = 0; index < ordered.Count; index++)
            {
                var occupant = ordered[index];
                spreadTotals.TryGetValue(occupant.RuntimeId, out var spreadTotal);
                var afterSpreadStacks = checked(occupant.PoisonStacks + spreadTotal);
                var afterHp = occupant.Hp;
                long requestedDamage = 0;
                LifecycleDeathPolicy? deathPolicy = null;
                var removed = false;

                if (sourceIds.Contains(occupant.RuntimeId) && occupant.SupportsHealth)
                {
                    requestedDamage = CalculateDamage(occupant.MaxHp, occupant.PoisonStacks);
                    afterHp = requestedDamage >= occupant.Hp
                        ? 0
                        : occupant.Hp - (int)requestedDamage;
                    if (afterHp == 0)
                    {
                        deathPolicy = _deathPolicyResolver.Resolve(occupant);
                        removed = deathPolicy == LifecycleDeathPolicy.Remove;
                    }
                }

                var skippedDecay = afterHp == 0 && occupant.SupportsHealth;
                var afterStacks = skippedDecay
                    ? 0
                    : sourceIds.Contains(occupant.RuntimeId)
                        ? Math.Max(0, afterSpreadStacks - 1)
                        : afterSpreadStacks;

                finalStates.Add(
                    occupant.RuntimeId,
                    new FinalState(afterHp, afterStacks, removed, deathPolicy, afterSpreadStacks));

                if (sourceIds.Contains(occupant.RuntimeId))
                {
                    damageResults.Add(new PoisonDamageResult(
                        sequence,
                        occupant,
                        requestedDamage,
                        afterHp,
                        deathPolicy,
                        removed));
                    decayResults.Add(new PoisonDecayResult(
                        sequence,
                        occupant,
                        afterSpreadStacks,
                        afterStacks,
                        skippedDecay));
                }
            }

            var spreadResults = new List<PoisonSpreadResult>(spreadPairs.Count);
            for (var index = 0; index < spreadPairs.Count; index++)
            {
                var pair = spreadPairs[index];
                spreadResults.Add(new PoisonSpreadResult(
                    sequence,
                    pair.Source,
                    pair.Target,
                    finalStates[pair.Target.RuntimeId].AfterSpreadStacks));
            }

            var mutations = new List<LifecycleOccupantMutation>();
            for (var index = 0; index < ordered.Count; index++)
            {
                var occupant = ordered[index];
                var final = finalStates[occupant.RuntimeId];
                if (!final.Removed &&
                    final.Hp == occupant.Hp &&
                    final.PoisonStacks == occupant.PoisonStacks)
                {
                    continue;
                }

                mutations.Add(new LifecycleOccupantMutation(
                    occupant.RuntimeId,
                    occupant.Coordinate,
                    TurnLifecyclePhase.ProcessingTurnStartStatuses,
                    LifecycleMutationReason.PoisonTurnStart,
                    occupant.Hp,
                    occupant.PoisonStacks,
                    final.Hp,
                    final.PoisonStacks,
                    final.Removed,
                    final.DeathPolicy));
            }

            if (mutations.Count > 0 && !_store.TryApply(mutations, out var failureReason))
            {
                return Fail(
                    sequence,
                    PoisonTurnStartFailure.StoreRejectedMutation,
                    string.IsNullOrWhiteSpace(failureReason)
                        ? "The lifecycle occupant store rejected Poison processing."
                        : failureReason);
            }

            var occupantChanges = new List<LifecycleOccupantChangeResult>(mutations.Count);
            for (var index = 0; index < mutations.Count; index++)
            {
                occupantChanges.Add(new LifecycleOccupantChangeResult(sequence, mutations[index]));
            }

            _completedSequences.Add(sequence);
            return Complete(
                sequence,
                PoisonTurnStartFailure.None,
                null,
                false,
                spreadResults,
                damageResults,
                decayResults,
                occupantChanges);
        }

        private PoisonTurnStartResult Fail(
            long sequence,
            PoisonTurnStartFailure failure,
            string failureReason)
        {
            return Complete(
                sequence,
                failure,
                failureReason,
                false,
                Array.Empty<PoisonSpreadResult>(),
                Array.Empty<PoisonDamageResult>(),
                Array.Empty<PoisonDecayResult>(),
                Array.Empty<LifecycleOccupantChangeResult>());
        }

        private PoisonTurnStartResult Complete(
            long sequence,
            PoisonTurnStartFailure failure,
            string failureReason,
            bool alreadyProcessed,
            IReadOnlyList<PoisonSpreadResult> spreadResults,
            IReadOnlyList<PoisonDamageResult> damageResults,
            IReadOnlyList<PoisonDecayResult> decayResults,
            IReadOnlyList<LifecycleOccupantChangeResult> occupantChanges)
        {
            LastResult = new PoisonTurnStartResult(
                sequence,
                failure,
                failureReason,
                alreadyProcessed,
                spreadResults,
                damageResults,
                decayResults,
                occupantChanges);
            return LastResult;
        }

        private static long CalculateDamage(int maxHp, int snapshotStacks)
        {
            var product = (long)maxHp * snapshotStacks;
            return (product + 9L) / 10L;
        }

        private static HexCoord Add(HexCoord left, HexCoord right)
        {
            return new HexCoord(left.Q + right.Q, left.R + right.R);
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

        private sealed class SpreadPair
        {
            public SpreadPair(CombatOccupantSnapshot source, CombatOccupantSnapshot target)
            {
                Source = source;
                Target = target;
            }

            public CombatOccupantSnapshot Source { get; }

            public CombatOccupantSnapshot Target { get; }
        }

        private sealed class FinalState
        {
            public FinalState(
                int hp,
                int poisonStacks,
                bool removed,
                LifecycleDeathPolicy? deathPolicy,
                int afterSpreadStacks)
            {
                Hp = hp;
                PoisonStacks = poisonStacks;
                Removed = removed;
                DeathPolicy = deathPolicy;
                AfterSpreadStacks = afterSpreadStacks;
            }

            public int Hp { get; }

            public int PoisonStacks { get; }

            public bool Removed { get; }

            public LifecycleDeathPolicy? DeathPolicy { get; }

            public int AfterSpreadStacks { get; }
        }
    }
}
