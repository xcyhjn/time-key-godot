using System;

namespace TimeKey.Domain.Intents
{
    public sealed class EnemyIntentResolver
    {
        public EnemyIntentResolveResult Resolve(
            EnemyIntentScheduledAction scheduled,
            EnemyIntentWorldSnapshot currentWorld)
        {
            if (scheduled == null)
            {
                throw new ArgumentNullException(nameof(scheduled));
            }

            if (currentWorld == null)
            {
                throw new ArgumentNullException(nameof(currentWorld));
            }

            var invalidReason = Revalidate(scheduled, currentWorld);
            var removal = new EnemyIntentRemovalSnapshot(
                scheduled.Action.ActionId,
                scheduled.OccupiedCells);
            if (invalidReason != EnemyIntentInvalidReason.None)
            {
                return new EnemyIntentResolveResult(
                    scheduled.Action.ActionId,
                    false,
                    EnemyIntentResolveOutcome.RemovedInvalid,
                    invalidReason,
                    removal);
            }

            var outcome = scheduled.Candidate.Source.Effect.SourceCommand == null
                ? EnemyIntentResolveOutcome.UnsupportedSourceCommand
                : EnemyIntentResolveOutcome.UnsupportedEffect;
            return new EnemyIntentResolveResult(
                scheduled.Action.ActionId,
                true,
                outcome,
                EnemyIntentInvalidReason.None,
                removal);
        }

        private static EnemyIntentInvalidReason Revalidate(
            EnemyIntentScheduledAction scheduled,
            EnemyIntentWorldSnapshot currentWorld)
        {
            var frozen = scheduled.Candidate.Source;
            if (!currentWorld.TryGetSource(frozen.RuntimeId, out var current))
            {
                return EnemyIntentInvalidReason.MissingSource;
            }

            if (current.Coordinate != frozen.Coordinate ||
                !current.IsAlive ||
                !current.CanGenerateIntent ||
                !string.Equals(
                    current.IntentStableId,
                    frozen.IntentStableId,
                    StringComparison.Ordinal))
            {
                return EnemyIntentInvalidReason.SourceUnavailable;
            }

            if (!TargetEqual(current.Target, frozen.Target))
            {
                return EnemyIntentInvalidReason.TargetUnavailable;
            }

            var targetReason = EnemyIntentRules.ValidateTarget(currentWorld, frozen.Target);
            if (targetReason != EnemyIntentInvalidReason.None)
            {
                return targetReason;
            }

            if (!EnemyIntentRules.SequenceEqual(
                    current.LiteralShape,
                    frozen.LiteralShape) ||
                !EnemyIntentRules.SequenceEqual(
                    scheduled.Action.Shape,
                    frozen.LiteralShape))
            {
                return EnemyIntentInvalidReason.ShapeMismatch;
            }

            if (!EffectEqual(current.Effect, frozen.Effect))
            {
                return EnemyIntentInvalidReason.EffectMismatch;
            }

            var sourceReason = EnemyIntentRules.ValidateSource(
                currentWorld,
                current,
                out var currentEffectRange);
            if (sourceReason != EnemyIntentInvalidReason.None)
            {
                return sourceReason;
            }

            if (!EnemyIntentRules.SequenceEqual(
                    currentEffectRange,
                    scheduled.Candidate.EffectRange))
            {
                return EnemyIntentInvalidReason.EffectMismatch;
            }

            if (scheduled.Action.ActionId != scheduled.Candidate.ActionId ||
                !EnemyIntentRules.SequenceEqual(
                    scheduled.Action.EffectRange,
                    scheduled.Candidate.EffectRange))
            {
                return EnemyIntentInvalidReason.EffectMismatch;
            }

            for (var index = 0; index < frozen.LiteralShape.Count; index++)
            {
                if (scheduled.OccupiedCells[index] !=
                    scheduled.Origin + frozen.LiteralShape[index])
                {
                    return EnemyIntentInvalidReason.ShapeMismatch;
                }
            }

            return EnemyIntentInvalidReason.None;
        }

        private static bool TargetEqual(
            EnemyIntentTargetSnapshot left,
            EnemyIntentTargetSnapshot right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.TargetId, right.TargetId, StringComparison.Ordinal) &&
                   left.Coordinate == right.Coordinate &&
                   left.RequiresOccupant == right.RequiresOccupant &&
                   string.Equals(
                       left.OccupantRuntimeId,
                       right.OccupantRuntimeId,
                       StringComparison.Ordinal) &&
                   left.RequiredAttitude == right.RequiredAttitude &&
                   left.RequiresLivingOccupant == right.RequiresLivingOccupant;
        }

        private static bool EffectEqual(
            EnemyIntentEffectSnapshot left,
            EnemyIntentEffectSnapshot right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return string.Equals(left.StableId, right.StableId, StringComparison.Ordinal) &&
                   string.Equals(left.SourceCommand, right.SourceCommand, StringComparison.Ordinal) &&
                   EnemyIntentRules.SequenceEqual(left.RangeOffsets, right.RangeOffsets);
        }
    }
}
