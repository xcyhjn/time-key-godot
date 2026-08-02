using System;
using TimeKey.Domain.BattleFlow;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.BattleFlow
{
    public sealed class BattleSettlementPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootCanvasGroup = null;
        [SerializeField] private Text title = null;
        [SerializeField] private Text detail = null;
        [SerializeField] private Button rewardButton = null;
        [SerializeField] private Text rewardButtonLabel = null;
        [SerializeField] private Font silverFont = null;

        private BattleRewardEntry _rewardEntry;
        private string _requestedRewardEntryId;

        public event Action<BattleRewardEntry> RewardRequested;

        public BattleSettlementSnapshot Snapshot { get; private set; }

        public bool IsVisible => rootCanvasGroup != null && rootCanvasGroup.alpha > 0f;

        public bool IsRewardVisible => rewardButton != null && rewardButton.gameObject.activeSelf;

        public void Apply(BattleSettlementSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ValidateDependencies();
            ApplySilverFont();
            BindRewardButton();
            Snapshot = snapshot;
            _rewardEntry = snapshot.RewardEntry;

            switch (snapshot.Outcome)
            {
                case BattleOutcome.Active:
                    _requestedRewardEntryId = null;
                    title.text = string.Empty;
                    detail.text = string.Empty;
                    SetRewardVisible(false);
                    SetVisible(false);
                    return;
                case BattleOutcome.VictorySettlement:
                    title.text = BattleFlowChineseText.Victory;
                    detail.text = snapshot.IsRewardClaimed
                        ? BattleFlowChineseText.RewardClaimed
                        : BattleFlowChineseText.VictorySettled;
                    ApplyVictoryReward(snapshot);
                    SetVisible(true);
                    return;
                case BattleOutcome.Defeat:
                    title.text = BattleFlowChineseText.Defeat;
                    detail.text = BattleFlowChineseText.BattleEnded;
                    SetRewardVisible(false);
                    SetVisible(true);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(snapshot),
                        snapshot.Outcome,
                        "Unsupported battle outcome.");
            }
        }

        private void ApplyVictoryReward(BattleSettlementSnapshot snapshot)
        {
            var canRequest = snapshot.RewardEntry != null &&
                             !snapshot.IsRewardClaimed &&
                             !string.Equals(
                                 _requestedRewardEntryId,
                                 snapshot.RewardEntry.EntryStableId,
                                 StringComparison.Ordinal);
            if (canRequest)
            {
                rewardButtonLabel.text = snapshot.RewardEntry.RewardLabel;
            }

            SetRewardVisible(canRequest);
        }

        private void HandleRewardRequested()
        {
            if (_rewardEntry == null || !IsRewardVisible || !rewardButton.interactable)
            {
                return;
            }

            _requestedRewardEntryId = _rewardEntry.EntryStableId;
            SetRewardVisible(false);
            RewardRequested?.Invoke(_rewardEntry);
        }

        private void BindRewardButton()
        {
            rewardButton.onClick.RemoveListener(HandleRewardRequested);
            rewardButton.onClick.AddListener(HandleRewardRequested);
        }

        private void SetRewardVisible(bool visible)
        {
            rewardButton.gameObject.SetActive(visible);
            rewardButton.interactable = visible;
        }

        private void SetVisible(bool visible)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        private void ApplySilverFont()
        {
            title.font = silverFont;
            detail.font = silverFont;
            rewardButtonLabel.font = silverFont;
        }

        private void ValidateDependencies()
        {
            if (rootCanvasGroup == null || title == null || detail == null ||
                rewardButton == null || rewardButtonLabel == null)
            {
                throw new InvalidOperationException(
                    name + " has incomplete serialized settlement references.");
            }

            if (silverFont == null ||
                !string.Equals(silverFont.name, "Silver", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(name + " must use the Silver font.");
            }
        }

        private void OnDisable()
        {
            if (rewardButton != null)
            {
                rewardButton.onClick.RemoveListener(HandleRewardRequested);
            }
        }
    }
}
