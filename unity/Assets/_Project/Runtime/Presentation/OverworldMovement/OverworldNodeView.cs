using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Presentation.Theming;

namespace TimeKey.Presentation.OverworldMovement
{
    public enum OverworldNodeVisualState
    {
        Idle,
        Current,
        Available,
        Locked,
        Settled,
        Moving,
        Arrived
    }

    [DisallowMultipleComponent]
    public sealed class OverworldNodeView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private string nodeId = "start";
        [SerializeField] private int q;
        [SerializeField] private int r;
        [SerializeField] private MapNodeType nodeType = MapNodeType.Normal;
        [SerializeField] private Button button;
        [SerializeField] private Graphic background;
        [SerializeField] private Text label;
        [SerializeField] private UiThemeBinder themeBinder;

        private OverworldNodeVisualState _state;

        public event Action<OverworldNodeView> Clicked;

        public MapNodeId Id => new MapNodeId(nodeId);
        public AxialHexCoord Coordinate => new AxialHexCoord(q, r);
        public MapNodeType NodeType => nodeType;
        public OverworldNodeVisualState State => _state;

        public void OnPointerDown(PointerEventData eventData)
        {
            GetComponentInParent<OverworldMapInputController>()?.BeginPointerGesture();
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
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void SetVisualState(OverworldNodeVisualState state)
        {
            _state = state;
            if (themeBinder != null)
            {
                themeBinder.Rebind();
            }

            if (button != null)
            {
                button.interactable = state == OverworldNodeVisualState.Available ||
                    state == OverworldNodeVisualState.Current;
            }

            if (label != null)
            {
                label.text = state == OverworldNodeVisualState.Current ? "当前位置" : nodeId;
            }

            var scope = GetComponentInParent<UiThemeScope>();
            if (scope != null && scope.TryGet(UiStyleId.OverworldNode, out var style))
            {
                if (background != null)
                {
                    background.color = ColorForState(style, state);
                }

                if (label != null)
                {
                    label.color = state == OverworldNodeVisualState.Locked
                        ? style.text.disabledColor
                        : style.text.normalColor;
                }
            }
        }

        private static Color ColorForState(
            TimeKeyUiTheme.UiStyleDefinition style,
            OverworldNodeVisualState state)
        {
            switch (state)
            {
                case OverworldNodeVisualState.Current:
                    return style.button.selectedColor;
                case OverworldNodeVisualState.Available:
                    return style.button.normalColor;
                case OverworldNodeVisualState.Moving:
                    return style.button.pressedColor;
                case OverworldNodeVisualState.Arrived:
                    return style.button.highlightedColor;
                case OverworldNodeVisualState.Settled:
                    return Color.Lerp(style.button.disabledColor, style.frame.borderColor, 0.45f);
                default:
                    return style.button.disabledColor;
            }
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }
    }
}
