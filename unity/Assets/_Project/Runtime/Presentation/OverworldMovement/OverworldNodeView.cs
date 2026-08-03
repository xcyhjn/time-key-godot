using System;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Presentation.Theming;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.OverworldMovement
{
    public enum OverworldNodeVisualState
    {
        Idle,
        Current,
        Available,
        Locked,
        Visited,
        Settled,
        Selected,
        Confirming,
        Moving,
        Arrived
    }

    [DisallowMultipleComponent]
    public sealed class OverworldNodeView : MonoBehaviour,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private string nodeId = "start";
        [SerializeField] private int q;
        [SerializeField] private int r;
        [SerializeField] private MapNodeType nodeType = MapNodeType.Normal;
        [SerializeField] private OverworldRoomType roomType = OverworldRoomType.Battle;
        [SerializeField] private Button button;
        [SerializeField] private Graphic background;
        [SerializeField] private Text label;
        [SerializeField] private UiThemeBinder themeBinder;

        private OverworldNodeVisualState _state;
        private string _displayLabel;
        private bool _hovered;
        private bool _focused;

        public event Action<OverworldNodeView> Clicked;

        public MapNodeId Id => new MapNodeId(nodeId);
        public AxialHexCoord Coordinate => new AxialHexCoord(q, r);
        public MapNodeType NodeType => nodeType;
        public OverworldRoomType RoomType => roomType;
        public OverworldNodeVisualState State => _state;
        public bool IsHovered => _hovered;
        public bool IsFocused => _focused;

        public void ConfigureRuntime(
            MapNodeId id,
            int layer,
            int slot,
            OverworldRoomType type,
            string displayLabel)
        {
            nodeId = id.Value;
            q = layer;
            r = slot;
            roomType = type;
            nodeType = LegacyNodeType(type);
            _displayLabel = string.IsNullOrWhiteSpace(displayLabel)
                ? RoomTypeText(type)
                : displayLabel;
            name = "Node-" + nodeId;
            RefreshVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GetComponentInParent<OverworldMapInputController>()?.BeginPointerGesture();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RefreshVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            RefreshVisuals();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            RefreshVisuals();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            RefreshVisuals();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (background == null)
            {
                background = GetComponent<Graphic>();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }

            if (string.IsNullOrWhiteSpace(_displayLabel))
            {
                _displayLabel = nodeId;
            }

            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }

            _hovered = false;
            _focused = false;
        }

        public void SetVisualState(OverworldNodeVisualState state)
        {
            _state = state;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (themeBinder != null)
            {
                themeBinder.Rebind();
            }

            if (button != null)
            {
                button.interactable = _state == OverworldNodeVisualState.Available ||
                    _state == OverworldNodeVisualState.Current;
            }

            if (label != null)
            {
                label.text = StateLabel();
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 18;
            }

            var scope = GetComponentInParent<UiThemeScope>();
            if (scope == null || !scope.TryGet(UiStyleId.OverworldNode, out var style))
            {
                return;
            }

            if (background != null)
            {
                var color = ColorForState(style, _state);
                if ((_hovered || _focused) && button != null && button.interactable)
                {
                    color = Color.Lerp(color, style.button.highlightedColor, 0.55f);
                }

                background.color = color;
            }

            if (label != null)
            {
                label.color = _state == OverworldNodeVisualState.Locked
                    ? style.text.disabledColor
                    : style.text.normalColor;
            }
        }

        private string StateLabel()
        {
            var baseLabel = string.IsNullOrWhiteSpace(_displayLabel)
                ? RoomTypeText(roomType)
                : _displayLabel;
            switch (_state)
            {
                case OverworldNodeVisualState.Current:
                    return baseLabel + "\n当前位置";
                case OverworldNodeVisualState.Available:
                    return baseLabel + "\n可前往";
                case OverworldNodeVisualState.Visited:
                    return baseLabel + "\n已到访";
                case OverworldNodeVisualState.Settled:
                    return baseLabel + "\n已结算";
                case OverworldNodeVisualState.Selected:
                    return baseLabel + "\n已选择";
                case OverworldNodeVisualState.Confirming:
                    return baseLabel + "\n确认中";
                case OverworldNodeVisualState.Moving:
                    return baseLabel + "\n移动中";
                case OverworldNodeVisualState.Locked:
                    return baseLabel + "\n未解锁";
                default:
                    return baseLabel;
            }
        }

        private static Color ColorForState(
            TimeKeyUiTheme.UiStyleDefinition style,
            OverworldNodeVisualState state)
        {
            switch (state)
            {
                case OverworldNodeVisualState.Current:
                case OverworldNodeVisualState.Selected:
                    return style.button.selectedColor;
                case OverworldNodeVisualState.Available:
                    return style.button.normalColor;
                case OverworldNodeVisualState.Moving:
                case OverworldNodeVisualState.Confirming:
                    return style.button.pressedColor;
                case OverworldNodeVisualState.Arrived:
                    return style.button.highlightedColor;
                case OverworldNodeVisualState.Visited:
                    return Color.Lerp(style.button.disabledColor, style.button.normalColor, 0.45f);
                case OverworldNodeVisualState.Settled:
                    return Color.Lerp(style.button.disabledColor, style.frame.borderColor, 0.45f);
                default:
                    return style.button.disabledColor;
            }
        }

        private static MapNodeType LegacyNodeType(OverworldRoomType type)
        {
            switch (type)
            {
                case OverworldRoomType.Entry:
                    return MapNodeType.Start;
                case OverworldRoomType.Elite:
                    return MapNodeType.Elite;
                case OverworldRoomType.Event:
                    return MapNodeType.Event;
                case OverworldRoomType.Boss:
                    return MapNodeType.Boss;
                default:
                    return MapNodeType.Normal;
            }
        }

        public static string RoomTypeText(OverworldRoomType type)
        {
            switch (type)
            {
                case OverworldRoomType.Entry:
                    return "章节入口";
                case OverworldRoomType.Elite:
                    return "精英战斗";
                case OverworldRoomType.Event:
                    return "时序事件";
                case OverworldRoomType.Shop:
                    return "时钥商店";
                case OverworldRoomType.Boss:
                    return "章节首领";
                default:
                    return "战斗房间";
            }
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }
    }
}
