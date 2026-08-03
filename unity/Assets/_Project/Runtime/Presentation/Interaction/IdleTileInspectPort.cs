using System;
using TimeKey.Domain;

namespace TimeKey.Presentation.Interaction
{
    public readonly struct IdleTileInspectState : IEquatable<IdleTileInspectState>
    {
        public IdleTileInspectState(HexCoord? coordinate)
        {
            Coordinate = coordinate;
        }

        public HexCoord? Coordinate { get; }

        public bool IsInspecting => Coordinate.HasValue;

        public bool Equals(IdleTileInspectState other)
        {
            return Nullable.Equals(Coordinate, other.Coordinate);
        }

        public override bool Equals(object obj)
        {
            return obj is IdleTileInspectState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Coordinate.HasValue ? Coordinate.Value.GetHashCode() : 0;
        }
    }

    public sealed class IdleTileInspectPort
    {
        private IdleTileInspectState _state;

        public event Action<IdleTileInspectState> Changed;

        public IdleTileInspectState State => _state;

        public bool Set(HexCoord coordinate)
        {
            return Set(new IdleTileInspectState(coordinate));
        }

        public bool Toggle(HexCoord coordinate)
        {
            if (_state.IsInspecting && _state.Coordinate.Value.Equals(coordinate))
            {
                return Clear();
            }

            return Set(coordinate);
        }

        public bool Clear()
        {
            if (!_state.IsInspecting)
            {
                return false;
            }

            _state = default;
            Changed?.Invoke(_state);
            return true;
        }

        public void ClearAll()
        {
            Clear();
        }

        private bool Set(IdleTileInspectState state)
        {
            if (_state.Equals(state))
            {
                return false;
            }

            _state = state;
            Changed?.Invoke(_state);
            return true;
        }
    }
}
