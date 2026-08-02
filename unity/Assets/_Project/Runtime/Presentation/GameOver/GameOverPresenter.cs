using System;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.GameOver
{
    [DisallowMultipleComponent]
    public sealed class GameOverPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootCanvasGroup = null;
        [SerializeField] private Text title = null;
        [SerializeField] private Text detail = null;
        [SerializeField] private Button returnButton = null;
        [SerializeField] private Text returnButtonLabel = null;
        [SerializeField] private Font silverFont = null;

        private bool _requested;

        public event Action ReturnRequested;

        public CombatOutcome Outcome { get; private set; }

        public bool IsBusy { get; private set; }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(HandleReturnClicked);
            }
        }

        public void Apply(CombatOutcome outcome)
        {
            if (outcome == null || outcome.ReturnPayload.Outcome != BattleOutcome.Defeat)
            {
                throw new ArgumentException("Game Over requires a typed defeat outcome.", nameof(outcome));
            }

            ValidateDependencies();
            ApplySilverFont();
            Outcome = outcome;
            _requested = false;
            title.text = "时序断裂";
            detail.text = "本次战斗失败\n种子 " + outcome.ReturnPayload.BattleSeed;
            SetBusy(false);
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
            Bind();
        }

        public void SetBusy(bool value)
        {
            IsBusy = value;
            if (!value)
            {
                _requested = false;
            }

            if (returnButton != null)
            {
                returnButton.interactable = !value;
            }
        }

        private void Bind()
        {
            if (returnButton == null)
            {
                return;
            }

            returnButton.onClick.RemoveListener(HandleReturnClicked);
            returnButton.onClick.AddListener(HandleReturnClicked);
        }

        private void HandleReturnClicked()
        {
            if (_requested || IsBusy || SceneInputLockState.IsLocked)
            {
                return;
            }

            _requested = true;
            SetBusy(true);
            ReturnRequested?.Invoke();
        }

        private void ApplySilverFont()
        {
            title.font = silverFont;
            detail.font = silverFont;
            returnButtonLabel.font = silverFont;
        }

        private void ValidateDependencies()
        {
            if (rootCanvasGroup == null || title == null || detail == null ||
                returnButton == null || returnButtonLabel == null)
            {
                throw new InvalidOperationException(
                    name + " has incomplete serialized Game Over references.");
            }

            if (silverFont == null ||
                !string.Equals(silverFont.name, "Silver", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(name + " must use the Silver font.");
            }
        }
    }
}
