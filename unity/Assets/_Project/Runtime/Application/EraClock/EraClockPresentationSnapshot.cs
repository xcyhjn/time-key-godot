using System;
using TimeKey.Application.BattleFlow;
using TimeKey.Application.SceneFlow;

namespace TimeKey.Application.EraClock
{
    public enum EraClockAnchorTarget
    {
        Center,
        Hud
    }

    public sealed class EraClockPresentationSnapshot : IEquatable<EraClockPresentationSnapshot>
    {
        public EraClockPresentationSnapshot(
            int era,
            int phase,
            long sequence,
            EraClockAnchorTarget anchorTarget)
            : this(
                era,
                phase,
                sequence,
                anchorTarget,
                isInputLocked: false,
                isRevealed: true)
        {
        }

        public EraClockPresentationSnapshot(
            int era,
            int phase,
            long sequence,
            EraClockAnchorTarget anchorTarget,
            bool isInputLocked,
            bool isRevealed)
        {
            if (era < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(era));
            }

            if (phase < 1 || phase > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (sequence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (!Enum.IsDefined(typeof(EraClockAnchorTarget), anchorTarget))
            {
                throw new ArgumentOutOfRangeException(nameof(anchorTarget));
            }

            Era = era;
            Phase = phase;
            Sequence = sequence;
            AnchorTarget = anchorTarget;
            IsInputLocked = isInputLocked;
            IsRevealed = isRevealed;
        }

        public int Era { get; }

        public int Phase { get; }

        public long Sequence { get; }

        public EraClockAnchorTarget AnchorTarget { get; }

        public bool IsInputLocked { get; }

        public bool IsRevealed { get; }

        public float NormalizedProgress => Phase / 8f;

        public bool HasSameVisualState(EraClockPresentationSnapshot other)
        {
            return other != null &&
                Era == other.Era &&
                Phase == other.Phase &&
                AnchorTarget == other.AnchorTarget &&
                IsRevealed == other.IsRevealed;
        }

        public bool Equals(EraClockPresentationSnapshot other)
        {
            return other != null &&
                Sequence == other.Sequence &&
                IsInputLocked == other.IsInputLocked &&
                HasSameVisualState(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EraClockPresentationSnapshot);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Era;
                hash = (hash * 397) ^ Phase;
                hash = (hash * 397) ^ Sequence.GetHashCode();
                hash = (hash * 397) ^ (int)AnchorTarget;
                hash = (hash * 397) ^ IsInputLocked.GetHashCode();
                hash = (hash * 397) ^ IsRevealed.GetHashCode();
                return hash;
            }
        }
    }

    public static class EraClockSnapshotAdapter
    {
        public static EraClockPresentationSnapshot FromBattleFlow(
            BattleFlowPresentationSnapshot source,
            long sequence,
            EraClockAnchorTarget anchorTarget)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new EraClockPresentationSnapshot(
                source.Era,
                source.Phase,
                sequence,
                anchorTarget,
                source.IsInputLocked,
                isRevealed: true);
        }

        public static EraClockPresentationSnapshot FromOutOfBattle(
            OutOfBattleShellState source,
            long sequence,
            EraClockAnchorTarget anchorTarget)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new EraClockPresentationSnapshot(
                source.Era,
                source.Phase,
                sequence,
                anchorTarget,
                isInputLocked: false,
                isRevealed: true);
        }

        public static EraClockPresentationSnapshot FromRunStart(
            RunStartPayload source,
            long sequence,
            EraClockAnchorTarget anchorTarget)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new EraClockPresentationSnapshot(
                source.Era,
                source.Phase,
                sequence,
                anchorTarget,
                isInputLocked: false,
                isRevealed: true);
        }
    }
}
