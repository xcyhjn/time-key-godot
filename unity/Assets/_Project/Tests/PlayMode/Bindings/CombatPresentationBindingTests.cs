using System;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Targeting;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Bindings
{
    public sealed class CombatPresentationBindingTests
    {
        [Test]
        public void BindTwice_ForwardsOneResolveIntent_AndUnbindStopsForwarding()
        {
            var rig = CreateRig();
            try
            {
                var resolveCount = 0;
                rig.Binding.ResolveRequested += () => resolveCount++;

                rig.Binding.Bind();
                rig.Binding.Bind();
                rig.ResolveButton.onClick.Invoke();
                Assert.That(resolveCount, Is.EqualTo(1));

                rig.Binding.Unbind();
                rig.ResolveButton.onClick.Invoke();
                Assert.That(resolveCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rig.Root);
            }
        }

        [Test]
        public void DisableEnable_RebindsOnceWithoutDuplicatingIntent()
        {
            var rig = CreateRig();
            try
            {
                var resolveCount = 0;
                rig.Binding.ResolveRequested += () => resolveCount++;

                rig.ResolveButton.onClick.Invoke();
                Assert.That(resolveCount, Is.EqualTo(1));

                rig.Binding.enabled = false;
                rig.Binding.enabled = true;
                rig.Binding.Bind();
                rig.ResolveButton.onClick.Invoke();
                Assert.That(resolveCount, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rig.Root);
            }
        }

        [Test]
        public void MissingSerializedPresenter_FailsBeforeBindingAndNamesTheField()
        {
            var root = new GameObject("MissingBinding");
            root.SetActive(false);
            try
            {
                var binding = root.AddComponent<CombatPresentationBinding>();
                var exception = Assert.Throws<InvalidOperationException>(() => binding.Bind());
                StringAssert.Contains("cardHandPresenter", exception.Message);
                StringAssert.Contains("MissingBinding", exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        internal static BindingRig CreateRig()
        {
            var root = new GameObject("PresentationBindingRig");
            root.SetActive(false);

            var handObject = CreateChild(root, "CardHand", typeof(RectTransform));
            var cardHand = handObject.AddComponent<TimeKey.Presentation.Cards.CardHandHost>();

            var rangeObject = CreateChild(root, "RangePreview");
            var rangePreview = rangeObject.AddComponent<BoardRangePreview>();

            var timelineObject = CreateChild(root, "TimelinePreview");
            var timelinePreview = timelineObject.AddComponent<TimelinePlacementPreview>();
            var clearTimelinePreview = timelineObject.AddComponent<ClearTimelinePreview>();
            var timelineCellObject = CreateChild(
                root,
                "TimelineCell",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            var timelineCell = timelineCellObject.AddComponent<TimeKey.Presentation.TimelineCellView>();
            var timelineLabel = CreateChild(
                timelineCellObject,
                "Label",
                typeof(RectTransform)).AddComponent<Text>();
            SetField(timelineCell, "button", timelineCellObject.GetComponent<Button>());
            SetField(timelineCell, "label", timelineLabel);

            var statusText = CreateChild(root, "Status", typeof(RectTransform)).AddComponent<Text>();
            var targetText = CreateChild(root, "Target", typeof(RectTransform)).AddComponent<Text>();
            var resolveObject = CreateChild(root, "Resolve", typeof(RectTransform), typeof(Image));
            var resolveButton = resolveObject.AddComponent<Button>();

            var cardPresenter = CreateChild(root, "CardHandPresenter").AddComponent<CardHandPresenter>();
            SetField(cardPresenter, "cardHand", cardHand);

            var rangePresenter = CreateChild(root, "BoardRangePresenter").AddComponent<BoardRangePresenter>();
            SetField(rangePresenter, "rangePreview", rangePreview);

            var timelinePresenter = CreateChild(root, "TimelinePresenter").AddComponent<TimelinePresenter>();
            SetField(timelinePresenter, "timelinePreview", timelinePreview);
            SetField(timelinePresenter, "clearTimelinePreview", clearTimelinePreview);
            SetField(
                timelinePresenter,
                "timelineCells",
                new System.Collections.Generic.List<TimeKey.Presentation.TimelineCellView> { timelineCell });

            var hudPresenter = CreateChild(root, "CombatHudPresenter").AddComponent<CombatHudPresenter>();
            SetField(hudPresenter, "statusText", statusText);
            SetField(hudPresenter, "targetText", targetText);
            SetField(hudPresenter, "resolveButton", resolveButton);

            var occupantPresenter = CreateChild(root, "CombatOccupantPresenter")
                .AddComponent<CombatOccupantPresenter>();

            var binding = root.AddComponent<CombatPresentationBinding>();
            SetField(binding, "cardHandPresenter", cardPresenter);
            SetField(binding, "boardRangePresenter", rangePresenter);
            SetField(binding, "timelinePresenter", timelinePresenter);
            SetField(binding, "hudPresenter", hudPresenter);
            SetField(binding, "occupantPresenter", occupantPresenter);

            root.SetActive(true);
            return new BindingRig(
                root,
                binding,
                rangePreview,
                timelinePreview,
                timelinePresenter,
                statusText,
                targetText,
                resolveButton);
        }

        internal static GameObject CreateChild(GameObject parent, string name, params Type[] components)
        {
            var child = components.Length == 0
                ? new GameObject(name)
                : new GameObject(name, components);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        internal static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing test field " + fieldName + ".");
            field.SetValue(target, value);
        }

        internal sealed class BindingRig
        {
            public BindingRig(
                GameObject root,
                CombatPresentationBinding binding,
                BoardRangePreview rangePreview,
                TimelinePlacementPreview timelinePreview,
                TimelinePresenter timelinePresenter,
                Text statusText,
                Text targetText,
                Button resolveButton)
            {
                Root = root;
                Binding = binding;
                RangePreview = rangePreview;
                TimelinePreview = timelinePreview;
                TimelinePresenter = timelinePresenter;
                StatusText = statusText;
                TargetText = targetText;
                ResolveButton = resolveButton;
            }

            public GameObject Root { get; }
            public CombatPresentationBinding Binding { get; }
            public BoardRangePreview RangePreview { get; }
            public TimelinePlacementPreview TimelinePreview { get; }
            public TimelinePresenter TimelinePresenter { get; }
            public Text StatusText { get; }
            public Text TargetText { get; }
            public Button ResolveButton { get; }
        }
    }
}
