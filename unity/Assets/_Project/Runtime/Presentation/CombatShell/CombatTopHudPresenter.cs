using System;
using TimeKey.Application;
using TimeKey.Application.SceneFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.CombatShell
{
    [DisallowMultipleComponent]
    public sealed class CombatTopHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text helpLabel = null;
        [SerializeField] private Text identityLabel = null;
        [SerializeField] private Text roundLabel = null;
        [SerializeField] private Text clockLabel = null;
        [SerializeField] private Text timecoinsLabel = null;
        [SerializeField] private Text deckZonesLabel = null;
        [SerializeField] private Text enemyHealthLabel = null;
        [SerializeField] private Button pauseButton = null;
        [SerializeField] private Button settingsButton = null;
        [SerializeField] private CanvasGroup modalGroup = null;
        [SerializeField] private Text modalTitle = null;
        [SerializeField] private Text modalBody = null;
        [SerializeField] private Button modalCloseButton = null;
        [SerializeField] private Font silverFont = null;

        private bool _isBound;
        private bool _snapshotLocked;
        private IDisposable _modalInputLock;
        private GameObject _previousSelection;

        public bool IsModalOpen => modalGroup != null && modalGroup.blocksRaycasts;

        public void Bind()
        {
            ValidateDependencies();
            if (_isBound)
            {
                return;
            }

            pauseButton.onClick.AddListener(ShowPause);
            settingsButton.onClick.AddListener(ShowSettings);
            modalCloseButton.onClick.AddListener(CloseModal);
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            pauseButton.onClick.RemoveListener(ShowPause);
            settingsButton.onClick.RemoveListener(ShowSettings);
            modalCloseButton.onClick.RemoveListener(CloseModal);
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var battleFlow = state.BattleFlow;
            Apply(new CombatTopHudDisplay(
                battleFlow == null ? 1 : battleFlow.Era,
                battleFlow == null ? 1 : battleFlow.Phase,
                battleFlow == null ? 0 : battleFlow.Timecoins,
                battleFlow == null ? 0 : battleFlow.DrawPile.Count,
                battleFlow == null ? 0 : battleFlow.Hand.Count,
                battleFlow == null ? 0 : battleFlow.DiscardPile.Count,
                state.TargetHp,
                state.TargetMaxHp,
                state.PlayerIdentityLabel,
                battleFlow != null && battleFlow.IsInputLocked ||
                state.Phase == CombatSessionPhase.Resolved ||
                state.Phase == CombatSessionPhase.Disposed));
        }

        public void Apply(CombatTopHudDisplay display)
        {
            if (display == null)
            {
                throw new ArgumentNullException(nameof(display));
            }

            ValidateDependencies();
            ApplySilverFont();
            helpLabel.text = "帮助";
            identityLabel.text = display.PlayerIdentityLabel;
            roundLabel.text = "时代 " + display.Era + "  /  阶段 " + display.Phase;
            clockLabel.text = display.Era.ToString("00") + " : " +
                display.Phase.ToString("00");
            timecoinsLabel.text = "时间币 " + display.Timecoins;
            deckZonesLabel.text = "牌库 " + display.DrawPileCount +
                "  手牌 " + display.HandCount +
                "  弃牌 " + display.DiscardPileCount;
            enemyHealthLabel.text = "敌方总生命  " + display.TargetHp + " / " +
                display.TargetMaxHp;
            _snapshotLocked = display.IsInputLocked;
            ApplyButtonState();
        }

        private void OnEnable()
        {
            if (DependenciesAssigned())
            {
                Bind();
                CloseModal(false);
            }
        }

        private void OnDisable()
        {
            CloseModal(false);
            Unbind();
        }

        private void Update()
        {
            if (IsModalOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseModal();
            }

            if (_isBound)
            {
                ApplyButtonState();
            }
        }

        private void ShowPause()
        {
            ShowModal("战斗暂停", "战场状态已保留。关闭面板后继续行动。");
        }

        private void ShowSettings()
        {
            ShowModal("战斗设置", "音频、画面与操作设置将在主设置页统一保存。");
        }

        private void ShowModal(string title, string body)
        {
            if (!IsModalOpen)
            {
                _previousSelection = EventSystem.current == null
                    ? null
                    : EventSystem.current.currentSelectedGameObject;
                _modalInputLock = SceneInputLockState.Acquire();
            }

            modalTitle.text = title;
            modalBody.text = body;
            modalGroup.alpha = 1f;
            modalGroup.interactable = true;
            modalGroup.blocksRaycasts = true;
            modalCloseButton.Select();
            ApplyButtonState();
        }

        private void CloseModal()
        {
            CloseModal(true);
        }

        private void CloseModal(bool restoreFocus)
        {
            if (modalGroup == null)
            {
                return;
            }

            modalGroup.alpha = 0f;
            modalGroup.interactable = false;
            modalGroup.blocksRaycasts = false;
            _modalInputLock?.Dispose();
            _modalInputLock = null;
            if (restoreFocus && EventSystem.current != null &&
                _previousSelection != null && _previousSelection.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_previousSelection);
            }

            _previousSelection = null;
            ApplyButtonState();
        }

        private void ApplyButtonState()
        {
            if (pauseButton == null || settingsButton == null)
            {
                return;
            }

            var enabled = !_snapshotLocked && !SceneInputLockState.IsLocked && !IsModalOpen;
            pauseButton.interactable = enabled;
            settingsButton.interactable = enabled;
        }

        private void ApplySilverFont()
        {
            helpLabel.font = silverFont;
            identityLabel.font = silverFont;
            roundLabel.font = silverFont;
            clockLabel.font = silverFont;
            timecoinsLabel.font = silverFont;
            deckZonesLabel.font = silverFont;
            enemyHealthLabel.font = silverFont;
            modalTitle.font = silverFont;
            modalBody.font = silverFont;
            SetButtonFont(pauseButton);
            SetButtonFont(settingsButton);
            SetButtonFont(modalCloseButton);
        }

        private void SetButtonFont(Button button)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.font = silverFont;
            }
        }

        private bool DependenciesAssigned()
        {
            return helpLabel != null && identityLabel != null && roundLabel != null &&
                clockLabel != null && timecoinsLabel != null && enemyHealthLabel != null &&
                deckZonesLabel != null &&
                pauseButton != null && settingsButton != null && modalGroup != null &&
                modalTitle != null && modalBody != null && modalCloseButton != null &&
                silverFont != null;
        }

        private void ValidateDependencies()
        {
            if (!DependenciesAssigned())
            {
                throw new InvalidOperationException(
                    name + " is missing a serialized Combat TopHUD reference.");
            }
        }
    }

    public sealed class CombatTopHudDisplay
    {
        public CombatTopHudDisplay(
            int era,
            int phase,
            int timecoins,
            int drawPileCount,
            int handCount,
            int discardPileCount,
            int targetHp,
            int targetMaxHp,
            string playerIdentityLabel,
            bool isInputLocked)
        {
            Era = era;
            Phase = phase;
            Timecoins = timecoins;
            DrawPileCount = drawPileCount;
            HandCount = handCount;
            DiscardPileCount = discardPileCount;
            TargetHp = targetHp;
            TargetMaxHp = targetMaxHp;
            PlayerIdentityLabel = playerIdentityLabel ?? string.Empty;
            IsInputLocked = isInputLocked;
        }

        public int Era { get; }

        public int Phase { get; }

        public int Timecoins { get; }

        public int DrawPileCount { get; }

        public int HandCount { get; }

        public int DiscardPileCount { get; }

        public int TargetHp { get; }

        public int TargetMaxHp { get; }

        public string PlayerIdentityLabel { get; }

        public bool IsInputLocked { get; }
    }
}
