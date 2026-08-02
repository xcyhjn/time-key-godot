using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.BattleFlow
{
    public sealed class BattleFlowPresenter : MonoBehaviour
    {
        [SerializeField] private Text drawPileCount = null;
        [SerializeField] private Text handCount = null;
        [SerializeField] private Text discardPileCount = null;
        [SerializeField] private Text roundLabel = null;
        [SerializeField] private Text timecoinsLabel = null;
        [SerializeField] private BattleSettlementPresenter settlementPresenter = null;
        [SerializeField] private Selectable[] combatInputControls = Array.Empty<Selectable>();
        [SerializeField] private Font silverFont = null;

        private IReadOnlyList<TimelineActionPresentationSnapshot> _actionDisplaySnapshots =
            Array.Empty<TimelineActionPresentationSnapshot>();
        private bool? _lastInputLock;

        public event Action<IReadOnlyList<TimelineActionPresentationSnapshot>>
            ActionDisplaySnapshotsChanged;

        public event Action<bool> InputLockChanged;

        public BattleFlowPresentationSnapshot Snapshot { get; private set; }

        public IReadOnlyList<TimelineActionPresentationSnapshot> ActionDisplaySnapshots =>
            _actionDisplaySnapshots;

        public bool IsInputLocked => Snapshot != null && Snapshot.IsInputLocked;

        public BattleSettlementPresenter SettlementPresenter => settlementPresenter;

        public void Apply(BattleFlowPresentationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ValidateDependencies();
            ApplySilverFont();

            drawPileCount.text = BattleFlowChineseText.Count(
                BattleFlowChineseText.DrawPile,
                snapshot.DrawPile.Count);
            handCount.text = BattleFlowChineseText.Count(
                BattleFlowChineseText.Hand,
                snapshot.Hand.Count);
            discardPileCount.text = BattleFlowChineseText.Count(
                BattleFlowChineseText.DiscardPile,
                snapshot.DiscardPile.Count);
            roundLabel.text = BattleFlowChineseText.Round(snapshot.Era, snapshot.Phase);
            timecoinsLabel.text = BattleFlowChineseText.Count(
                BattleFlowChineseText.Timecoins,
                snapshot.Timecoins);

            Snapshot = snapshot;
            ApplyInputLock(snapshot.IsInputLocked);
            settlementPresenter.Apply(snapshot.Settlement);
            ApplyActionDisplaySnapshots(snapshot.ActionDisplaySnapshots);
        }

        private void ApplyInputLock(bool isLocked)
        {
            if (isLocked)
            {
                for (var index = 0; index < combatInputControls.Length; index++)
                {
                    if (combatInputControls[index] != null)
                    {
                        combatInputControls[index].interactable = false;
                    }
                }
            }

            if (_lastInputLock == isLocked)
            {
                return;
            }

            _lastInputLock = isLocked;
            InputLockChanged?.Invoke(isLocked);
        }

        private void ApplyActionDisplaySnapshots(
            IReadOnlyList<TimelineActionPresentationSnapshot> snapshots)
        {
            if (HasSameSnapshotReferences(snapshots))
            {
                return;
            }

            var copied = new List<TimelineActionPresentationSnapshot>(snapshots.Count);
            for (var index = 0; index < snapshots.Count; index++)
            {
                copied.Add(snapshots[index] ??
                    throw new ArgumentException(
                        "Action display snapshots cannot contain null values.",
                        nameof(snapshots)));
            }

            _actionDisplaySnapshots = new ReadOnlyCollection<TimelineActionPresentationSnapshot>(copied);
            ActionDisplaySnapshotsChanged?.Invoke(_actionDisplaySnapshots);
        }

        private bool HasSameSnapshotReferences(
            IReadOnlyList<TimelineActionPresentationSnapshot> snapshots)
        {
            if (_actionDisplaySnapshots.Count != snapshots.Count)
            {
                return false;
            }

            for (var index = 0; index < snapshots.Count; index++)
            {
                if (!ReferenceEquals(_actionDisplaySnapshots[index], snapshots[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplySilverFont()
        {
            drawPileCount.font = silverFont;
            handCount.font = silverFont;
            discardPileCount.font = silverFont;
            roundLabel.font = silverFont;
            timecoinsLabel.font = silverFont;
        }

        private void ValidateDependencies()
        {
            if (drawPileCount == null || handCount == null || discardPileCount == null ||
                roundLabel == null || timecoinsLabel == null || settlementPresenter == null)
            {
                throw new InvalidOperationException(
                    name + " has incomplete serialized battle-flow references.");
            }

            if (silverFont == null ||
                !string.Equals(silverFont.name, "Silver", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(name + " must use the Silver font.");
            }
        }
    }
}
