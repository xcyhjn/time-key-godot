using System;
using System.Collections.Generic;

namespace TimeKey.Domain
{
    public sealed class CombatSliceState : ILifecycleOccupantStore
    {
        private static readonly HexCoord LegacyTargetCoordinate = new HexCoord(1, 0);

        private readonly Dictionary<string, CombatOccupantState> _occupantsById =
            new Dictionary<string, CombatOccupantState>(StringComparer.Ordinal);
        private readonly Dictionary<HexCoord, CombatOccupantState> _occupantsByCoordinate =
            new Dictionary<HexCoord, CombatOccupantState>();
        private readonly int _targetMaxHp;

        public CombatSliceState(string targetId, int targetHp, int seed, int turn = 1)
            : this(targetId, targetHp, seed, new CombatBoardState(), turn)
        {
        }

        public CombatSliceState(
            string targetId,
            int targetHp,
            int seed,
            CombatBoardState board,
            int turn = 1)
            : this(
                targetId,
                seed,
                board,
                CreateLegacyOccupants(targetId, targetHp),
                turn)
        {
        }

        public CombatSliceState(
            string targetId,
            int seed,
            CombatBoardState board,
            IReadOnlyList<CombatOccupantState> occupants,
            int turn = 1)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("A target ID is required.", nameof(targetId));
            }

            if (occupants == null)
            {
                throw new ArgumentNullException(nameof(occupants));
            }

            if (turn < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turn));
            }

            Board = board ?? throw new ArgumentNullException(nameof(board));

            TargetId = targetId.Trim();
            Seed = seed;
            Turn = turn;

            for (var index = 0; index < occupants.Count; index++)
            {
                if (!TryAddOccupant(occupants[index]))
                {
                    throw new ArgumentException(
                        "Occupants must have unique runtime IDs and coordinates.",
                        nameof(occupants));
                }
            }

            _targetMaxHp = _occupantsById.TryGetValue(TargetId, out var target) &&
                           target.SupportsHealth
                ? target.MaxHp
                : 0;
        }

        public string TargetId { get; }

        public int TargetHp
        {
            get
            {
                return _occupantsById.TryGetValue(TargetId, out var target) && target.SupportsHealth
                    ? target.Hp
                    : 0;
            }
        }

        public int TargetMaxHp => _targetMaxHp;

        public int Seed { get; }

        public int Turn { get; }

        public CombatBoardState Board { get; }

        public int OccupantCount => _occupantsById.Count;

        public IReadOnlyList<CombatOccupantSnapshot> CaptureOccupants()
        {
            var occupants = new List<CombatOccupantSnapshot>(_occupantsById.Count);
            foreach (var state in _occupantsById.Values)
            {
                occupants.Add(state.Snapshot());
            }

            occupants.Sort(CompareOccupants);
            return occupants.AsReadOnly();
        }

        public bool TryApply(
            IReadOnlyList<LifecycleOccupantMutation> mutations,
            out string failureReason)
        {
            if (mutations == null)
            {
                throw new ArgumentNullException(nameof(mutations));
            }

            var runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < mutations.Count; index++)
            {
                var mutation = mutations[index];
                if (mutation == null || !runtimeIds.Add(mutation.RuntimeId) ||
                    !TryGetMutableOccupant(
                        mutation == null ? null : mutation.RuntimeId,
                        mutation == null ? default : mutation.Coordinate,
                        out var occupant) ||
                    occupant.Hp != mutation.ExpectedHp ||
                    occupant.PoisonStacks != mutation.ExpectedPoisonStacks ||
                    (occupant.SupportsHealth && mutation.AfterHp > occupant.MaxHp) ||
                    (!occupant.SupportsHealth && mutation.AfterHp != 0) ||
                    (!occupant.SupportsStatus && mutation.AfterPoisonStacks != 0))
                {
                    failureReason = "Lifecycle occupant mutation preconditions changed.";
                    return false;
                }
            }

            for (var index = 0; index < mutations.Count; index++)
            {
                var mutation = mutations[index];
                var occupant = _occupantsById[mutation.RuntimeId];
                if (mutation.Remove)
                {
                    _occupantsById.Remove(occupant.RuntimeId);
                    _occupantsByCoordinate.Remove(occupant.Coordinate);
                    continue;
                }

                if ((occupant.SupportsHealth && !occupant.TrySetHealth(mutation.AfterHp)) ||
                    (occupant.SupportsStatus &&
                     !occupant.TrySetPoisonStacks(mutation.AfterPoisonStacks)))
                {
                    throw new InvalidOperationException(
                        "A validated lifecycle occupant mutation could not be applied.");
                }
            }

            failureReason = null;
            return true;
        }

        public bool TryGetOccupant(
            string runtimeId,
            HexCoord coordinate,
            out CombatOccupantSnapshot occupant)
        {
            occupant = null;
            if (string.IsNullOrWhiteSpace(runtimeId) ||
                !_occupantsByCoordinate.TryGetValue(coordinate, out var state) ||
                !string.Equals(state.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                return false;
            }

            occupant = state.Snapshot();
            return true;
        }

        public bool TryGetOccupant(HexCoord coordinate, out CombatOccupantSnapshot occupant)
        {
            occupant = null;
            if (!_occupantsByCoordinate.TryGetValue(coordinate, out var state))
            {
                return false;
            }

            occupant = state.Snapshot();
            return true;
        }

        public bool TryGetOccupant(string runtimeId, out CombatOccupantSnapshot occupant)
        {
            occupant = null;
            if (string.IsNullOrWhiteSpace(runtimeId) ||
                !_occupantsById.TryGetValue(runtimeId, out var state))
            {
                return false;
            }

            occupant = state.Snapshot();
            return true;
        }

        public bool TryAddOccupant(CombatOccupantState occupant)
        {
            if (occupant == null)
            {
                throw new ArgumentNullException(nameof(occupant));
            }

            if (_occupantsById.ContainsKey(occupant.RuntimeId) ||
                _occupantsByCoordinate.ContainsKey(occupant.Coordinate))
            {
                return false;
            }

            var owned = occupant.Copy();
            _occupantsById.Add(owned.RuntimeId, owned);
            _occupantsByCoordinate.Add(owned.Coordinate, owned);
            return true;
        }

        public bool TryRemoveOccupant(string runtimeId, HexCoord coordinate)
        {
            if (!TryGetMutableOccupant(runtimeId, coordinate, out var occupant))
            {
                return false;
            }

            _occupantsById.Remove(occupant.RuntimeId);
            _occupantsByCoordinate.Remove(occupant.Coordinate);
            return true;
        }

        public bool TrySetOccupantHealth(string runtimeId, HexCoord coordinate, int hp)
        {
            if (!TryGetMutableOccupant(runtimeId, coordinate, out var occupant))
            {
                return false;
            }

            return occupant.TrySetHealth(hp);
        }

        public bool TryAddOccupantPoisonStacks(
            string runtimeId,
            HexCoord coordinate,
            int amount)
        {
            if (!TryGetMutableOccupant(runtimeId, coordinate, out var occupant))
            {
                return false;
            }

            return occupant.TryAddPoisonStacks(amount);
        }

        internal void ApplyDamage(string targetId, int damage)
        {
            if (string.IsNullOrWhiteSpace(targetId) ||
                damage <= 0 ||
                !_occupantsById.TryGetValue(targetId, out var target) ||
                !target.SupportsHealth)
            {
                return;
            }

            target.TrySetHealth(Math.Max(0, target.Hp - damage));
        }

        private bool TryGetMutableOccupant(
            string runtimeId,
            HexCoord coordinate,
            out CombatOccupantState occupant)
        {
            occupant = null;
            return !string.IsNullOrWhiteSpace(runtimeId) &&
                   _occupantsByCoordinate.TryGetValue(coordinate, out occupant) &&
                   string.Equals(occupant.RuntimeId, runtimeId, StringComparison.Ordinal);
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

        private static IReadOnlyList<CombatOccupantState> CreateLegacyOccupants(
            string targetId,
            int targetHp)
        {
            if (targetHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetHp));
            }

            return new[]
            {
                new CombatOccupantState(
                    targetId,
                    LegacyTargetCoordinate,
                    "entity",
                    CombatAttitude.Enemy,
                    targetHp,
                    Math.Max(100, targetHp),
                    poisonStacks: 0,
                    supportsHealth: true,
                    supportsStatus: true)
            };
        }
    }
}
