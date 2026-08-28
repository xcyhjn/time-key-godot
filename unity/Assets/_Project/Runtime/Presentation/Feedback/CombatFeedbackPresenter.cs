using System;
using System.Collections.Generic;
using System.Threading;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Intents;
using UnityEngine;

namespace TimeKey.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private CombatFeedbackVfxPool vfxPool = null;

        private CombatFeedbackProjector _projector;

        public event Action<CombatFeedbackEvent> FeedbackQueued;

        public CombatFeedbackVfxPool VfxPool => vfxPool;

        public CombatFeedbackProjector Projector => _projector;

        public int ActiveVfxCount => vfxPool == null ? 0 : vfxPool.ActiveCount;

        private void Awake()
        {
            _projector = new CombatFeedbackProjector();
        }

        public void Initialize(
            CombatFeedbackProjector projector,
            CombatFeedbackVfxPool pool = null)
        {
            _projector = projector ?? throw new ArgumentNullException(nameof(projector));
            if (pool != null)
            {
                vfxPool = pool;
            }
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayTrace(
            CombatTraceEntry entry,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectTrace(entry, cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayCommand(
            string command,
            CombatCommandResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectCommand(command, result, cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayResolution(
            ResolutionSnapshot snapshot,
            string sourceId,
            string targetId = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectResolution(snapshot, sourceId, targetId, cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayResolution(
            ResolutionSnapshot snapshot,
            string sourceId,
            string targetId,
            CombatFeedbackWorldAnchor worldAnchor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectResolution(
                snapshot,
                sourceId,
                targetId,
                worldAnchor,
                cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayLifecycle(
            TurnLifecycleResult result,
            IReadOnlyList<LifecycleOccupantChangeResult> changes,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectLifecycle(result, changes, cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayEnemyIntent(
            EnemyIntentResolveResult result,
            string sourceId,
            string targetId = null,
            HexCoord? targetCoordinate = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectEnemyIntent(
                result,
                sourceId,
                targetId,
                targetCoordinate,
                cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlaySettlement(
            BattleSettlementResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectBattleSettlement(result, cancellationToken));
        }

        public IReadOnlyList<CombatFeedbackEvent> PlayBattleFlow(
            BattleFlowHookResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Dispatch(_projector.ProjectBattleFlow(result, cancellationToken));
        }

        public CombatFeedbackEvent PlaySceneTransition(
            string sourceId,
            string targetId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var value = _projector.ProjectSceneTransition(sourceId, targetId, cancellationToken);
            Dispatch(new[] { value });
            return value;
        }

        private IReadOnlyList<CombatFeedbackEvent> Dispatch(
            IReadOnlyList<CombatFeedbackEvent> events)
        {
            if (events == null)
            {
                return Array.Empty<CombatFeedbackEvent>();
            }

            for (var index = 0; index < events.Count; index++)
            {
                var value = events[index];
                if (value == null)
                {
                    continue;
                }

                FeedbackQueued?.Invoke(value);
                if (vfxPool != null)
                {
                    vfxPool.Play(value);
                }
            }

            return events;
        }
    }
}
