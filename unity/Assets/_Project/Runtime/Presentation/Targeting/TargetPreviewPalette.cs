using UnityEngine;

namespace TimeKey.Presentation.Targeting
{
    public static class TargetPreviewPalette
    {
        public static Color Normal => new Color(0.28f, 0.33f, 0.30f, 1f);

        public static Color Hover => new Color(0.42f, 0.64f, 0.92f, 1f);

        public static Color Selected => new Color(0.24f, 0.78f, 0.82f, 1f);

        public static Color RangeValid => new Color(0.96f, 0.76f, 0.18f, 1f);

        public static Color TimelineValid => new Color(0.24f, 0.78f, 0.42f, 1f);

        public static Color TimelineInvalid => new Color(0.92f, 0.24f, 0.27f, 1f);
    }
}
