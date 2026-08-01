using System;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Tooltips
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CardEffectFrame : MonoBehaviour
    {
        [SerializeField] private Image background = null;
        [SerializeField] private Image actorStripe = null;
        [SerializeField] private Text title = null;
        [SerializeField] private Text description = null;
        [SerializeField] private Text metadata = null;
        [SerializeField] private CanvasGroup canvasGroup = null;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.5f;

        public string DisplayTitle => title == null ? null : title.text;

        public void ShowCard(CardViewModel card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            ValidateDependencies();
            title.text = card.DisplayName;
            description.text = card.EffectDescription;
            metadata.text = card.PlacementDescription;
            background.color = new Color(0.055f, 0.075f, 0.078f, 0.98f);
            actorStripe.color = new Color(0.18f, 0.76f, 0.72f, 1f);
            SetVisible(true);
        }

        public void ShowAction(TimelineActionPresentationSnapshot action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            ValidateDependencies();
            var enemy = action.ActorKind == TimelineActorKind.Enemy;
            title.text = (enemy ? "敌方意图  " : "玩家行动  ") + action.Display.Title;
            description.text = action.Display.Description;
            metadata.text = BuildActionMetadata(action);
            background.color = enemy
                ? new Color(0.12f, 0.055f, 0.06f, 0.98f)
                : new Color(0.045f, 0.085f, 0.09f, 0.98f);
            actorStripe.color = enemy
                ? new Color(0.92f, 0.31f, 0.28f, 1f)
                : new Color(0.18f, 0.76f, 0.72f, 1f);
            SetVisible(true);
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                SetVisible(false);
            }
        }

        private static string BuildActionMetadata(TimelineActionPresentationSnapshot action)
        {
            var state = action.Validity == TimelineActionValidity.Valid
                ? "已排程"
                : action.InvalidReason == TimelineActionInvalidReason.UnsupportedSourceCommand
                    ? "无效果 / 命令暂不支持"
                    : "失效 / " + action.InvalidReason;
            var source = action.Display.SourceLabel ?? "来源：玩家";
            var target = action.Display.TargetLabel ?? "目标：地块";
            return source + "  |  " + target + "\n" +
                "占格：" + action.OccupiedCells.Count + "  |  " + state;
        }

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void ValidateDependencies()
        {
            if (background == null || actorStripe == null || title == null ||
                description == null || metadata == null || canvasGroup == null)
            {
                throw new InvalidOperationException(name + " has incomplete serialized effect-frame references.");
            }
        }
    }
}
