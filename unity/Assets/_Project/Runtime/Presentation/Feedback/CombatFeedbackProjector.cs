using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Intents;
using UnityEngine;

namespace TimeKey.Presentation.Feedback
{
    public sealed class CombatFeedbackProjector
    {
        private long _nextSequence;

        public CombatFeedbackProjector(long sequenceFloor = 0)
        {
            if (sequenceFloor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequenceFloor));
            }

            _nextSequence = sequenceFloor;
        }

        public long NextSequence => _nextSequence + 1;

        public CombatFeedbackEvent ProjectCardDraw(
            string sourceId,
            string targetId = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Create(
                sourceId,
                targetId,
                default(CombatFeedbackWorldAnchor),
                CombatFeedbackKind.CardDraw,
                CombatFeedbackPhase.Playing,
                0.2f,
                cancellationToken);
        }

        public CombatFeedbackEvent ProjectCardConfirm(
            string sourceId,
            string targetId,
            CombatFeedbackWorldAnchor worldAnchor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Create(
                sourceId,
                targetId,
                worldAnchor,
                CombatFeedbackKind.CardConfirm,
                CombatFeedbackPhase.Playing,
                0.25f,
                cancellationToken);
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectTrace(
            CombatTraceEntry entry,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var kind = KindForTrace(entry);
            if (!kind.HasValue)
            {
                return Empty();
            }

            var anchor = entry.TargetCoordinate.HasValue
                ? CombatFeedbackWorldAnchor.FromCoordinate(entry.TargetCoordinate.Value, entry.TargetId)
                : default(CombatFeedbackWorldAnchor);
            var noEffect = kind.Value == CombatFeedbackKind.UnsupportedSourceCommand;
            var phase = noEffect
                ? CombatFeedbackPhase.Completed
                : entry.Command == "resolve-effect"
                    ? CombatFeedbackPhase.Playing
                    : CombatFeedbackPhase.Requested;
            var duration = noEffect ? 0f : DurationFor(kind.Value);
            return Single(Create(
                string.IsNullOrWhiteSpace(entry.StableId) ? entry.Command : entry.StableId,
                entry.TargetId,
                anchor,
                kind.Value,
                phase,
                duration,
                cancellationToken,
                noEffect));
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectCommand(
            string command,
            CombatCommandResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("A command is required.", nameof(command));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var kind = KindForCommand(command, result);
            if (!kind.HasValue)
            {
                return Empty();
            }

            var anchor = result.Target.HasValue
                ? CombatFeedbackWorldAnchor.FromCoordinate(result.Target.Value.Coordinate, result.Target.Value.EntityId)
                : default(CombatFeedbackWorldAnchor);
            return Single(Create(
                string.IsNullOrWhiteSpace(result.StableId) ? command : result.StableId,
                result.Target.HasValue && result.Target.Value.Kind == CombatTargetKind.Entity
                    ? result.Target.Value.EntityId
                    : null,
                anchor,
                kind.Value,
                result.Succeeded ? CombatFeedbackPhase.Requested : CombatFeedbackPhase.Completed,
                result.Succeeded ? DurationFor(kind.Value) : 0f,
                cancellationToken,
                kind.Value == CombatFeedbackKind.UnsupportedSourceCommand));
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectResolution(
            ResolutionSnapshot snapshot,
            string sourceId,
            string targetId = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return ProjectResolution(
                snapshot,
                sourceId,
                targetId,
                default(CombatFeedbackWorldAnchor),
                cancellationToken);
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectResolution(
            ResolutionSnapshot snapshot,
            string sourceId,
            string targetId,
            CombatFeedbackWorldAnchor worldAnchor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var events = new List<CombatFeedbackEvent>();
            if (snapshot.TargetHpAfter < snapshot.TargetHpBefore)
            {
                events.Add(Create(
                    sourceId,
                    targetId,
                    worldAnchor,
                    CombatFeedbackKind.Damage,
                    CombatFeedbackPhase.Playing,
                    DurationFor(CombatFeedbackKind.Damage),
                    cancellationToken));
            }
            else if (snapshot.TargetHpAfter > snapshot.TargetHpBefore)
            {
                events.Add(Create(
                    sourceId,
                    targetId,
                    worldAnchor,
                    CombatFeedbackKind.Recover,
                    CombatFeedbackPhase.Playing,
                    DurationFor(CombatFeedbackKind.Recover),
                    cancellationToken));
            }

            for (var index = 0; index < snapshot.EffectResults.Count; index++)
            {
                var effect = snapshot.EffectResults[index];
                events.Add(Create(
                    sourceId,
                    targetId,
                    CombatFeedbackWorldAnchor.FromCoordinate(effect.Coordinate),
                    CombatFeedbackKind.ElevationPulse,
                    CombatFeedbackPhase.Playing,
                    DurationFor(CombatFeedbackKind.ElevationPulse),
                    cancellationToken));
            }

            for (var index = 0; index < snapshot.OccupantEffectResults.Count; index++)
            {
                var effect = snapshot.OccupantEffectResults[index];
                if (effect == null || effect.After == null)
                {
                    continue;
                }

                var kind = KindForEffect(effect.EffectKind);
                events.Add(Create(
                    sourceId,
                    effect.After.RuntimeId,
                    CombatFeedbackWorldAnchor.FromCoordinate(effect.After.Coordinate, effect.After.RuntimeId),
                    kind,
                    CombatFeedbackPhase.Playing,
                    DurationFor(kind),
                    cancellationToken));
            }

            return new ReadOnlyCollection<CombatFeedbackEvent>(events);
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectLifecycle(
            TurnLifecycleResult result,
            IReadOnlyList<LifecycleOccupantChangeResult> changes,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var events = new List<CombatFeedbackEvent>();
            if (changes == null)
            {
                return Empty();
            }

            for (var index = 0; index < changes.Count; index++)
            {
                var change = changes[index];
                if (change == null)
                {
                    continue;
                }

                var kind = change.Reason == LifecycleMutationReason.TowerDecay
                    ? CombatFeedbackKind.TowerDecay
                    : CombatFeedbackKind.PoisonTick;
                events.Add(Create(
                    change.Reason.ToString(),
                    change.RuntimeId,
                    CombatFeedbackWorldAnchor.FromCoordinate(change.Coordinate, change.RuntimeId),
                    kind,
                    CombatFeedbackPhase.Playing,
                    DurationFor(kind),
                    cancellationToken));
            }

            return new ReadOnlyCollection<CombatFeedbackEvent>(events);
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectEnemyIntent(
            EnemyIntentResolveResult result,
            string sourceId,
            string targetId = null,
            HexCoord? targetCoordinate = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var kind = result.Outcome == EnemyIntentResolveOutcome.UnsupportedSourceCommand
                ? CombatFeedbackKind.UnsupportedSourceCommand
                : CombatFeedbackKind.EnemyIntentResolve;
            var anchor = targetCoordinate.HasValue
                ? CombatFeedbackWorldAnchor.FromCoordinate(targetCoordinate.Value, targetId)
                : default(CombatFeedbackWorldAnchor);
            return Single(Create(
                sourceId,
                targetId,
                anchor,
                kind,
                CombatFeedbackPhase.Completed,
                0f,
                cancellationToken,
                kind == CombatFeedbackKind.UnsupportedSourceCommand));
        }

        public CombatFeedbackEvent ProjectEnemyIntentHover(
            string sourceId,
            string targetId,
            HexCoord targetCoordinate,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Create(
                sourceId,
                targetId,
                CombatFeedbackWorldAnchor.FromCoordinate(targetCoordinate, targetId),
                CombatFeedbackKind.EnemyIntentHover,
                CombatFeedbackPhase.Playing,
                DurationFor(CombatFeedbackKind.EnemyIntentHover),
                cancellationToken);
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectBattleSettlement(
            BattleSettlementResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Succeeded || result.Snapshot == null)
            {
                return Empty();
            }

            var kind = result.Snapshot.Outcome == BattleOutcome.VictorySettlement
                ? CombatFeedbackKind.Victory
                : CombatFeedbackKind.Defeat;
            return Single(Create(
                result.Snapshot.BattleTag,
                null,
                default(CombatFeedbackWorldAnchor),
                kind,
                CombatFeedbackPhase.Playing,
                DurationFor(kind),
                cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> ProjectBattleFlow(
            BattleFlowHookResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var events = new List<CombatFeedbackEvent>();
            if (!result.Succeeded)
            {
                return Empty();
            }

            if (result.RoundResult != null &&
                result.RoundResult.Succeeded &&
                result.RoundResult.Before != null &&
                result.RoundResult.After != null &&
                result.RoundResult.Before.Era != result.RoundResult.After.Era)
            {
                events.Add(Create(
                    "clock-rollover",
                    null,
                    default(CombatFeedbackWorldAnchor),
                    CombatFeedbackKind.ClockRollover,
                    CombatFeedbackPhase.Playing,
                    DurationFor(CombatFeedbackKind.ClockRollover),
                    cancellationToken));
            }

            if (result.DrawResult != null && result.DrawResult.Succeeded)
            {
                if (result.DrawResult.Shuffle != null && result.DrawResult.Shuffle.Occurred)
                {
                    events.Add(Create(
                        "deck-shuffle",
                        null,
                        default(CombatFeedbackWorldAnchor),
                        CombatFeedbackKind.Shuffle,
                        CombatFeedbackPhase.Playing,
                        DurationFor(CombatFeedbackKind.Shuffle),
                        cancellationToken));
                }

                if (result.DrawResult.MovedCount > 0)
                {
                    events.Add(Create(
                        "deck-draw",
                        null,
                        default(CombatFeedbackWorldAnchor),
                        CombatFeedbackKind.CardDraw,
                        CombatFeedbackPhase.Playing,
                        DurationFor(CombatFeedbackKind.CardDraw),
                        cancellationToken));
                }
            }

            return new ReadOnlyCollection<CombatFeedbackEvent>(events);
        }

        public CombatFeedbackEvent ProjectSceneTransition(
            string sourceId,
            string targetId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Create(
                sourceId,
                targetId,
                default(CombatFeedbackWorldAnchor),
                CombatFeedbackKind.SceneTransition,
                CombatFeedbackPhase.Playing,
                DurationFor(CombatFeedbackKind.SceneTransition),
                cancellationToken);
        }

        private CombatFeedbackEvent Create(
            string sourceId,
            string targetId,
            CombatFeedbackWorldAnchor worldAnchor,
            CombatFeedbackKind kind,
            CombatFeedbackPhase phase,
            float duration,
            CancellationToken cancellationToken,
            bool noEffect = false)
        {
            var normalizedSource = string.IsNullOrWhiteSpace(sourceId)
                ? kind.ToString()
                : sourceId.Trim();
            return new CombatFeedbackEvent(
                checked(++_nextSequence),
                normalizedSource,
                targetId,
                worldAnchor,
                kind,
                phase,
                duration,
                cancellationToken,
                noEffect);
        }

        private static IReadOnlyList<CombatFeedbackEvent> Single(CombatFeedbackEvent value)
        {
            return new ReadOnlyCollection<CombatFeedbackEvent>(
                new List<CombatFeedbackEvent> { value });
        }

        private static IReadOnlyList<CombatFeedbackEvent> Empty()
        {
            return Array.Empty<CombatFeedbackEvent>();
        }

        private static CombatFeedbackKind? KindForCommand(
            string command,
            CombatCommandResult result)
        {
            switch (command.Trim())
            {
                case "draw-card":
                case "draw":
                    return CombatFeedbackKind.CardDraw;
                case "commit-timeline":
                    return CombatFeedbackKind.CardConfirm;
                case "commit-clear":
                    return CombatFeedbackKind.Clear;
                case "resolve-effect":
                    return KindForEffect(result.Resolution == null
                        ? (CardEffectKind?)null
                        : FirstEffectKind(result.Resolution));
                case "clock-rollover":
                    return CombatFeedbackKind.ClockRollover;
                case "shuffle":
                    return CombatFeedbackKind.Shuffle;
                case "scene-transition":
                    return CombatFeedbackKind.SceneTransition;
                case "unsupported-source-command":
                    return CombatFeedbackKind.UnsupportedSourceCommand;
                default:
                    return null;
            }
        }

        private static CombatFeedbackKind? KindForTrace(CombatTraceEntry entry)
        {
            switch (entry.Command)
            {
                case "draw-card":
                case "draw":
                    return CombatFeedbackKind.CardDraw;
                case "commit-timeline":
                    return CombatFeedbackKind.CardConfirm;
                case "commit-clear":
                    return CombatFeedbackKind.Clear;
                case "resolve-effect":
                    return KindForEffect(entry.EffectKind);
                case "clock-rollover":
                    return CombatFeedbackKind.ClockRollover;
                case "shuffle":
                    return CombatFeedbackKind.Shuffle;
                case "scene-transition":
                    return CombatFeedbackKind.SceneTransition;
                case "enemy-intent-hover":
                    return CombatFeedbackKind.EnemyIntentHover;
                case "enemy-intent-resolve":
                    return CombatFeedbackKind.EnemyIntentResolve;
                case "unsupported-source-command":
                    return CombatFeedbackKind.UnsupportedSourceCommand;
                default:
                    return null;
            }
        }

        private static CombatFeedbackKind KindForEffect(CardEffectKind? effectKind)
        {
            if (!effectKind.HasValue)
            {
                return CombatFeedbackKind.UnsupportedSourceCommand;
            }

            switch (effectKind.Value)
            {
                case CardEffectKind.Damage:
                    return CombatFeedbackKind.Damage;
                case CardEffectKind.Elevation:
                    return CombatFeedbackKind.ElevationPulse;
                case CardEffectKind.Recover:
                    return CombatFeedbackKind.Recover;
                case CardEffectKind.Built:
                    return CombatFeedbackKind.Built;
                case CardEffectKind.Poison:
                    return CombatFeedbackKind.PoisonApply;
                case CardEffectKind.Clear:
                    return CombatFeedbackKind.Clear;
                default:
                    return CombatFeedbackKind.UnsupportedSourceCommand;
            }
        }

        private static CardEffectKind? FirstEffectKind(ResolutionSnapshot snapshot)
        {
            if (snapshot.OccupantEffectResults.Count > 0 &&
                snapshot.OccupantEffectResults[0] != null)
            {
                return snapshot.OccupantEffectResults[0].EffectKind;
            }

            return snapshot.EffectResults.Count > 0
                ? CardEffectKind.Elevation
                : snapshot.TargetHpAfter < snapshot.TargetHpBefore
                    ? CardEffectKind.Damage
                    : snapshot.TargetHpAfter > snapshot.TargetHpBefore
                        ? CardEffectKind.Recover
                        : (CardEffectKind?)null;
        }

        private static float DurationFor(CombatFeedbackKind kind)
        {
            switch (kind)
            {
                case CombatFeedbackKind.CardDraw:
                    return 0.2f;
                case CombatFeedbackKind.CardConfirm:
                    return 0.25f;
                case CombatFeedbackKind.Damage:
                    return 0.35f;
                case CombatFeedbackKind.ElevationPulse:
                    return 0.4f;
                case CombatFeedbackKind.Recover:
                    return 0.45f;
                case CombatFeedbackKind.Built:
                    return 0.5f;
                case CombatFeedbackKind.TowerDecay:
                case CombatFeedbackKind.PoisonApply:
                case CombatFeedbackKind.PoisonTick:
                    return 0.35f;
                case CombatFeedbackKind.Clear:
                    return 0.3f;
                case CombatFeedbackKind.EnemyIntentHover:
                    return 0.15f;
                case CombatFeedbackKind.EnemyIntentResolve:
                    return 0.25f;
                case CombatFeedbackKind.Victory:
                case CombatFeedbackKind.Defeat:
                    return 0.8f;
                case CombatFeedbackKind.ClockRollover:
                    return 0.75f;
                case CombatFeedbackKind.Shuffle:
                    return 0.3f;
                case CombatFeedbackKind.SceneTransition:
                    return 0.45f;
                default:
                    return 0f;
            }
        }
    }
}
