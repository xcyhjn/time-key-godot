using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle
{
    internal sealed class TestLifecycleOccupantStore : ILifecycleOccupantStore
    {
        private Dictionary<string, OccupantRecord> _records =
            new Dictionary<string, OccupantRecord>(StringComparer.Ordinal);

        public TestLifecycleOccupantStore(params CombatOccupantState[] occupants)
        {
            for (var index = 0; index < occupants.Length; index++)
            {
                var occupant = occupants[index];
                _records.Add(occupant.RuntimeId, new OccupantRecord(occupant));
            }
        }

        public int CaptureCalls { get; private set; }

        public int ApplyCalls { get; private set; }

        public bool RejectNextApply { get; set; }

        public IReadOnlyList<LifecycleOccupantMutation> LastMutations { get; private set; } =
            Array.Empty<LifecycleOccupantMutation>();

        public IReadOnlyList<CombatOccupantSnapshot> CaptureOccupants()
        {
            CaptureCalls++;
            var snapshots = new List<CombatOccupantSnapshot>(_records.Count);
            foreach (var pair in _records)
            {
                snapshots.Add(CreateSnapshot(pair.Value));
            }

            return new ReadOnlyCollection<CombatOccupantSnapshot>(snapshots);
        }

        public bool TryApply(
            IReadOnlyList<LifecycleOccupantMutation> mutations,
            out string failureReason)
        {
            ApplyCalls++;
            LastMutations = Copy(mutations);
            if (RejectNextApply)
            {
                RejectNextApply = false;
                failureReason = "fixture rejected mutation";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < mutations.Count; index++)
            {
                var mutation = mutations[index];
                if (!seen.Add(mutation.RuntimeId) ||
                    !_records.TryGetValue(mutation.RuntimeId, out var record) ||
                    record.Coordinate != mutation.Coordinate ||
                    record.Hp != mutation.ExpectedHp ||
                    record.PoisonStacks != mutation.ExpectedPoisonStacks)
                {
                    failureReason = "fixture precondition mismatch";
                    return false;
                }
            }

            var next = Clone(_records);
            for (var index = 0; index < mutations.Count; index++)
            {
                var mutation = mutations[index];
                if (mutation.Remove)
                {
                    next.Remove(mutation.RuntimeId);
                    continue;
                }

                var record = next[mutation.RuntimeId];
                record.Hp = mutation.AfterHp;
                record.PoisonStacks = mutation.AfterPoisonStacks;
            }

            _records = next;
            failureReason = null;
            return true;
        }

        public bool Contains(string runtimeId)
        {
            return _records.ContainsKey(runtimeId);
        }

        public CombatOccupantSnapshot Get(string runtimeId)
        {
            return CreateSnapshot(_records[runtimeId]);
        }

        public static CombatOccupantState Occupant(
            string runtimeId,
            int q,
            int r,
            int hp = 100,
            int maxHp = 100,
            int poisonStacks = 0,
            bool supportsHealth = true,
            bool supportsStatus = true,
            string kind = "enemy",
            string creationId = null)
        {
            return new CombatOccupantState(
                runtimeId,
                new HexCoord(q, r),
                kind,
                CombatAttitude.Enemy,
                hp,
                maxHp,
                poisonStacks,
                supportsHealth,
                supportsStatus,
                creationId);
        }

        private static CombatOccupantSnapshot CreateSnapshot(OccupantRecord record)
        {
            var state = new CombatSliceState(
                record.RuntimeId,
                seed: 731,
                new CombatBoardState(),
                new[] { record.ToState() });
            state.TryGetOccupant(record.RuntimeId, record.Coordinate, out var snapshot);
            return snapshot;
        }

        private static Dictionary<string, OccupantRecord> Clone(
            IReadOnlyDictionary<string, OccupantRecord> source)
        {
            var result = new Dictionary<string, OccupantRecord>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                result.Add(pair.Key, new OccupantRecord(pair.Value));
            }

            return result;
        }

        private static IReadOnlyList<LifecycleOccupantMutation> Copy(
            IReadOnlyList<LifecycleOccupantMutation> source)
        {
            var result = new List<LifecycleOccupantMutation>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return new ReadOnlyCollection<LifecycleOccupantMutation>(result);
        }

        private sealed class OccupantRecord
        {
            public OccupantRecord(CombatOccupantState state)
            {
                RuntimeId = state.RuntimeId;
                Coordinate = state.Coordinate;
                Kind = state.Kind;
                CreationId = state.CreationId;
                Attitude = state.Attitude;
                Hp = state.Hp;
                MaxHp = state.MaxHp;
                PoisonStacks = state.PoisonStacks;
                SupportsHealth = state.SupportsHealth;
                SupportsStatus = state.SupportsStatus;
            }

            public OccupantRecord(OccupantRecord source)
            {
                RuntimeId = source.RuntimeId;
                Coordinate = source.Coordinate;
                Kind = source.Kind;
                CreationId = source.CreationId;
                Attitude = source.Attitude;
                Hp = source.Hp;
                MaxHp = source.MaxHp;
                PoisonStacks = source.PoisonStacks;
                SupportsHealth = source.SupportsHealth;
                SupportsStatus = source.SupportsStatus;
            }

            public string RuntimeId { get; }
            public HexCoord Coordinate { get; }
            public string Kind { get; }
            public string CreationId { get; }
            public CombatAttitude Attitude { get; }
            public int Hp { get; set; }
            public int MaxHp { get; }
            public int PoisonStacks { get; set; }
            public bool SupportsHealth { get; }
            public bool SupportsStatus { get; }

            public CombatOccupantState ToState()
            {
                return new CombatOccupantState(
                    RuntimeId,
                    Coordinate,
                    Kind,
                    Attitude,
                    Hp,
                    MaxHp,
                    PoisonStacks,
                    SupportsHealth,
                    SupportsStatus,
                    CreationId);
            }
        }
    }
}
