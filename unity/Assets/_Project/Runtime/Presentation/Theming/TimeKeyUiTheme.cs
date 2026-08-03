using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Theming
{
    public enum UiStyleId
    {
        Panel,
        PrimaryButton,
        SecondaryButton,
        DangerButton,
        TopHud,
        OverworldNode,
        ConfirmationDialog,
        CardEffectFrame,
        PlayerActionFrame,
        EnemyIntentFrame,
        TimelineCell,
        Tooltip
    }

    [CreateAssetMenu(
        fileName = "TimeKeyDefaultUiTheme",
        menuName = "TimeKey/UI/Theme")]
    public sealed class TimeKeyUiTheme : ScriptableObject
    {
        [Serializable]
        public sealed class UiTextStyle
        {
            public Font font;
            public int fontSize = 24;
            public Color normalColor = Color.white;
            public Color disabledColor = new Color(0.55f, 0.58f, 0.58f, 1f);
            public Color outlineColor = Color.black;
            public int outlineSize;
        }

        [Serializable]
        public sealed class UiFrameStyle
        {
            public Sprite backgroundSprite;
            public Image.Type imageType = Image.Type.Sliced;
            public Color fillColor = Color.white;
            public Color borderColor = Color.clear;
            public Vector4 borderWidth;
            public Vector4 padding;
            public Vector4 visualOverflow;
            public Vector4 cornerRadius;
            public Color shadowColor = Color.clear;
            public Vector2 shadowOffset;
            public float shadowSize;
        }

        [Serializable]
        public sealed class UiButtonStyle
        {
            public UiTextStyle text = new UiTextStyle();
            public UiFrameStyle frame = new UiFrameStyle();
            public Color normalColor = Color.white;
            public Color highlightedColor = Color.white;
            public Color pressedColor = Color.white;
            public Color selectedColor = Color.white;
            public Color disabledColor = new Color(0.4f, 0.42f, 0.42f, 0.75f);
        }

        [Serializable]
        public sealed class UiStyleDefinition
        {
            public UiStyleId id;
            public UiTextStyle text = new UiTextStyle();
            public UiFrameStyle frame = new UiFrameStyle();
            public UiButtonStyle button = new UiButtonStyle();
        }

        [SerializeField] private UiStyleDefinition[] styles =
        {
            new UiStyleDefinition { id = UiStyleId.Panel },
            new UiStyleDefinition { id = UiStyleId.PrimaryButton },
            new UiStyleDefinition { id = UiStyleId.SecondaryButton },
            new UiStyleDefinition { id = UiStyleId.DangerButton },
            new UiStyleDefinition { id = UiStyleId.TopHud },
            new UiStyleDefinition { id = UiStyleId.OverworldNode },
            new UiStyleDefinition { id = UiStyleId.ConfirmationDialog },
            new UiStyleDefinition { id = UiStyleId.CardEffectFrame },
            new UiStyleDefinition { id = UiStyleId.PlayerActionFrame },
            new UiStyleDefinition { id = UiStyleId.EnemyIntentFrame },
            new UiStyleDefinition { id = UiStyleId.TimelineCell },
            new UiStyleDefinition { id = UiStyleId.Tooltip }
        };

        public IReadOnlyList<UiStyleDefinition> Styles => styles;

        public bool TryGet(UiStyleId id, out UiStyleDefinition style)
        {
            if (styles != null)
            {
                for (var index = 0; index < styles.Length; index++)
                {
                    if (styles[index] != null && styles[index].id == id)
                    {
                        style = styles[index];
                        return true;
                    }
                }
            }

            style = null;
            return false;
        }
    }
}
