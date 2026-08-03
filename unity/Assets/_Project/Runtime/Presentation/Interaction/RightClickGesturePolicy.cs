using System;

namespace TimeKey.Presentation.Interaction
{
    public readonly struct PointerPoint : IEquatable<PointerPoint>
    {
        public PointerPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public bool Equals(PointerPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is PointerPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }

    public enum RightClickGestureResult
    {
        Rejected,
        ShortClick,
        Drag
    }

    public sealed class RightClickGesturePolicy
    {
        private readonly float _thresholdSquared;
        private PointerPoint _start;
        private bool _active;
        private bool _dragged;

        public RightClickGesturePolicy(float thresholdPixels = 8f)
        {
            if (thresholdPixels <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(thresholdPixels));
            }

            ThresholdPixels = thresholdPixels;
            _thresholdSquared = thresholdPixels * thresholdPixels;
        }

        public float ThresholdPixels { get; }

        public bool IsActive => _active;

        public bool Begin(PointerPoint start, bool pointerOverInterface)
        {
            if (pointerOverInterface || _active)
            {
                _active = false;
                _dragged = false;
                return false;
            }

            _start = start;
            _active = true;
            _dragged = false;
            return true;
        }

        public bool Move(PointerPoint current)
        {
            if (!_active)
            {
                return false;
            }

            var dx = current.X - _start.X;
            var dy = current.Y - _start.Y;
            if ((dx * dx) + (dy * dy) > _thresholdSquared)
            {
                _dragged = true;
            }

            return _dragged;
        }

        public RightClickGestureResult End(PointerPoint current, bool pointerOverInterface)
        {
            if (!_active)
            {
                return RightClickGestureResult.Rejected;
            }

            Move(current);
            _active = false;
            if (pointerOverInterface)
            {
                _dragged = false;
                return RightClickGestureResult.Rejected;
            }

            var result = _dragged
                ? RightClickGestureResult.Drag
                : RightClickGestureResult.ShortClick;
            _dragged = false;
            return result;
        }

        public void Cancel()
        {
            _active = false;
            _dragged = false;
        }
    }
}
