using System;
using System.Threading;
using TimeKey.Domain;
using UnityEngine;

namespace TimeKey.Presentation.Feedback
{
    public enum CombatFeedbackKind
    {
        CardDraw,
        CardConfirm,
        Damage,
        ElevationPulse,
        Recover,
        Built,
        TowerDecay,
        PoisonApply,
        PoisonTick,
        Clear,
        EnemyIntentHover,
        EnemyIntentResolve,
        UnsupportedSourceCommand,
        Victory,
        Defeat,
        ClockRollover,
        Shuffle,
        SceneTransition
    }

    public enum CombatFeedbackPhase
    {
        Requested,
        Playing,
        Completed,
        Cancelled
    }

    public readonly struct CombatFeedbackWorldAnchor : IEquatable<CombatFeedbackWorldAnchor>
    {
        public CombatFeedbackWorldAnchor(
            HexCoord? coordinate,
            Vector3 worldPosition,
            string anchorId = null)
        {
            Coordinate = coordinate;
            WorldPosition = worldPosition;
            AnchorId = string.IsNullOrWhiteSpace(anchorId) ? null : anchorId.Trim();
        }

        public HexCoord? Coordinate { get; }

        public Vector3 WorldPosition { get; }

        public string AnchorId { get; }

        public bool HasCoordinate => Coordinate.HasValue;

        public bool HasWorldPosition => WorldPosition != Vector3.zero;

        public bool Equals(CombatFeedbackWorldAnchor other)
        {
            return Coordinate.Equals(other.Coordinate) &&
                   WorldPosition == other.WorldPosition &&
                   string.Equals(AnchorId, other.AnchorId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CombatFeedbackWorldAnchor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Coordinate.GetHashCode();
                hash = (hash * 397) ^ WorldPosition.GetHashCode();
                hash = (hash * 397) ^ (AnchorId == null
                    ? 0
                    : StringComparer.Ordinal.GetHashCode(AnchorId));
                return hash;
            }
        }

        public static CombatFeedbackWorldAnchor FromCoordinate(
            HexCoord coordinate,
            string anchorId = null)
        {
            return new CombatFeedbackWorldAnchor(coordinate, Vector3.zero, anchorId);
        }

        public static CombatFeedbackWorldAnchor FromWorldPosition(
            Vector3 worldPosition,
            string anchorId = null)
        {
            return new CombatFeedbackWorldAnchor(null, worldPosition, anchorId);
        }

        public override string ToString()
        {
            if (HasCoordinate)
            {
                return AnchorId == null
                    ? Coordinate.Value.ToString()
                    : AnchorId + "@" + Coordinate.Value;
            }

            return AnchorId == null
                ? WorldPosition.ToString()
                : AnchorId + "@" + WorldPosition;
        }
    }

    public sealed class CombatFeedbackEvent
    {
        public CombatFeedbackEvent(
            long sequence,
            string sourceId,
            string targetId,
            CombatFeedbackWorldAnchor worldAnchor,
            CombatFeedbackKind kind,
            CombatFeedbackPhase phase,
            float duration,
            CancellationToken cancellationToken = default(CancellationToken),
            bool noEffect = false)
        {
            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("A feedback source ID is required.", nameof(sourceId));
            }

            if (!Enum.IsDefined(typeof(CombatFeedbackKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!Enum.IsDefined(typeof(CombatFeedbackPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Sequence = sequence;
            SourceId = sourceId.Trim();
            TargetId = string.IsNullOrWhiteSpace(targetId) ? null : targetId.Trim();
            WorldAnchor = worldAnchor;
            Kind = kind;
            Phase = phase;
            Duration = duration;
            CancellationToken = cancellationToken;
            IsNoEffect = noEffect || kind == CombatFeedbackKind.UnsupportedSourceCommand;
        }

        public long Sequence { get; }

        public string SourceId { get; }

        public string TargetId { get; }

        public CombatFeedbackWorldAnchor WorldAnchor { get; }

        public CombatFeedbackKind Kind { get; }

        public CombatFeedbackPhase Phase { get; }

        public float Duration { get; }

        public CancellationToken CancellationToken { get; }

        public bool IsCancellable => CancellationToken.CanBeCanceled;

        public bool IsNoEffect { get; }

        public CombatFeedbackEvent Cancel()
        {
            return new CombatFeedbackEvent(
                Sequence,
                SourceId,
                TargetId,
                WorldAnchor,
                Kind,
                CombatFeedbackPhase.Cancelled,
                0f,
                CancellationToken,
                IsNoEffect);
        }
    }
}
