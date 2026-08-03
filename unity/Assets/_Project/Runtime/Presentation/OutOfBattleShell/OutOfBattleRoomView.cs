using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TimeKey.Presentation.Theming;

namespace TimeKey.Presentation.OutOfBattleShell
{
    public enum OutOfBattleRoomState
    {
        Idle,
        Hovered,
        Selected,
        Confirming,
        Settled,
        Disabled
    }

    [DisallowMultipleComponent]
    public sealed class OutOfBattleRoomView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private Button roomButton = null;
        [SerializeField] private Graphic background = null;
        [SerializeField] private Text titleLabel = null;
        [SerializeField] private Text statusLabel = null;
        [SerializeField] private UiThemeScope themeScope = null;
        [SerializeField] private bool usePanelStateColors;

        private bool _bound;
        private bool _hovered;
        private bool _focused;
        private bool _selected;
        private bool _confirming;
        private bool _settled;
        private bool _interactionLocked;

        public event Action SelectionRequested;

        public OutOfBattleRoomState State
        {
            get
            {
                if (_settled)
                {
                    return OutOfBattleRoomState.Settled;
                }

                if (_interactionLocked)
                {
                    return OutOfBattleRoomState.Disabled;
                }

                if (_confirming)
                {
                    return OutOfBattleRoomState.Confirming;
                }

                if (_selected)
                {
                    return OutOfBattleRoomState.Selected;
                }

                return _hovered || _focused
                    ? OutOfBattleRoomState.Hovered
                    : OutOfBattleRoomState.Idle;
            }
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Configure(string roomTitle)
        {
            if (titleLabel != null)
            {
                titleLabel.text = string.IsNullOrWhiteSpace(roomTitle)
                    ? "战斗房间"
                    : roomTitle;
            }

            Refresh();
        }

        public void ApplyFont(Font font)
        {
            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            if (titleLabel != null)
            {
                titleLabel.font = font;
            }

            if (statusLabel != null)
            {
                statusLabel.font = font;
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected && !_settled;
            if (!_selected)
            {
                _confirming = false;
            }

            Refresh();
        }

        public void SetConfirming(bool confirming)
        {
            _confirming = confirming && _selected && !_settled;
            Refresh();
        }

        public void SetSettled(bool settled)
        {
            _settled = settled;
            if (settled)
            {
                _selected = false;
                _confirming = false;
                _hovered = false;
                _focused = false;
            }

            Refresh();
        }

        public void SetInteractionLocked(bool locked)
        {
            _interactionLocked = locked;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            Refresh();
        }

        private void Bind()
        {
            if (_bound || roomButton == null)
            {
                return;
            }

            roomButton.onClick.AddListener(OnRoomClicked);
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound || roomButton == null)
            {
                return;
            }

            roomButton.onClick.RemoveListener(OnRoomClicked);
            _bound = false;
        }

        private void OnRoomClicked()
        {
            if (_settled || _interactionLocked || _confirming)
            {
                return;
            }

            SelectionRequested?.Invoke();
        }

        private void Refresh()
        {
            if (roomButton == null || background == null || statusLabel == null)
            {
                return;
            }

            var state = State;
            if (ApplyTheme(state))
            {
                return;
            }

            roomButton.interactable = state != OutOfBattleRoomState.Confirming &&
                state != OutOfBattleRoomState.Settled &&
                state != OutOfBattleRoomState.Disabled;
            switch (state)
            {
                case OutOfBattleRoomState.Hovered:
                    background.color = new Color(0.50f, 0.35f, 0.10f, 0.96f);
                    statusLabel.text = "查看战斗";
                    break;
                case OutOfBattleRoomState.Selected:
                    background.color = new Color(0.12f, 0.40f, 0.36f, 0.98f);
                    statusLabel.text = "已选择";
                    break;
                case OutOfBattleRoomState.Confirming:
                    background.color = new Color(0.18f, 0.50f, 0.46f, 1f);
                    statusLabel.text = "正在进入";
                    break;
                case OutOfBattleRoomState.Settled:
                    background.color = new Color(0.18f, 0.20f, 0.20f, 0.82f);
                    statusLabel.text = "已完成";
                    break;
                case OutOfBattleRoomState.Disabled:
                    background.color = new Color(0.12f, 0.13f, 0.14f, 0.72f);
                    statusLabel.text = "暂不可用";
                    break;
                default:
                    background.color = new Color(0.035f, 0.05f, 0.06f, 0.92f);
                    statusLabel.text = "可进入";
                    break;
            }
        }

        private bool ApplyTheme(OutOfBattleRoomState state)
        {
            if (themeScope == null)
            {
                themeScope = GetComponentInParent<UiThemeScope>();
            }

            if (themeScope == null ||
                !themeScope.TryGet(UiStyleId.OverworldNode, out var style))
            {
                return false;
            }

            roomButton.interactable = state != OutOfBattleRoomState.Confirming &&
                state != OutOfBattleRoomState.Settled &&
                state != OutOfBattleRoomState.Disabled;
            background.color = usePanelStateColors
                ? PanelColorFor(state)
                : ColorFor(state, style);
            titleLabel.color = style.text.normalColor;
            statusLabel.color = state == OutOfBattleRoomState.Disabled
                ? style.text.disabledColor
                : style.text.normalColor;
            if (style.text.font != null)
            {
                titleLabel.font = style.text.font;
                statusLabel.font = style.text.font;
            }

            switch (state)
            {
                case OutOfBattleRoomState.Hovered:
                    statusLabel.text = "查看战斗";
                    break;
                case OutOfBattleRoomState.Selected:
                    statusLabel.text = "已选择";
                    break;
                case OutOfBattleRoomState.Confirming:
                    statusLabel.text = "正在进入";
                    break;
                case OutOfBattleRoomState.Settled:
                    statusLabel.text = "已完成";
                    break;
                case OutOfBattleRoomState.Disabled:
                    statusLabel.text = "暂不可用";
                    break;
                default:
                    statusLabel.text = "可进入";
                    break;
            }

            return true;
        }

        private static Color ColorFor(
            OutOfBattleRoomState state,
            TimeKeyUiTheme.UiStyleDefinition style)
        {
            switch (state)
            {
                case OutOfBattleRoomState.Hovered:
                    return style.button.highlightedColor;
                case OutOfBattleRoomState.Selected:
                    return style.button.selectedColor;
                case OutOfBattleRoomState.Confirming:
                    return style.button.pressedColor;
                case OutOfBattleRoomState.Settled:
                case OutOfBattleRoomState.Disabled:
                    return style.button.disabledColor;
                default:
                    return style.frame.fillColor;
            }
        }

        private static Color PanelColorFor(OutOfBattleRoomState state)
        {
            switch (state)
            {
                case OutOfBattleRoomState.Hovered:
                    return new Color(0.13f, 0.24f, 0.22f, 0.98f);
                case OutOfBattleRoomState.Selected:
                    return new Color(0.09f, 0.29f, 0.26f, 0.98f);
                case OutOfBattleRoomState.Confirming:
                    return new Color(0.08f, 0.36f, 0.32f, 1f);
                case OutOfBattleRoomState.Settled:
                    return new Color(0.12f, 0.14f, 0.14f, 0.92f);
                case OutOfBattleRoomState.Disabled:
                    return new Color(0.08f, 0.10f, 0.11f, 0.86f);
                default:
                    return new Color(0.08f, 0.18f, 0.17f, 0.96f);
            }
        }
    }
}
