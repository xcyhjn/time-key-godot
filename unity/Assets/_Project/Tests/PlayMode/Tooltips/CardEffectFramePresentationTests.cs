using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Tooltips;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Tooltips
{
    public sealed class CardEffectFramePresentationTests
    {
        private GameObject _root;
        private Sprite _sprite;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            if (_sprite != null)
            {
                UnityEngine.Object.Destroy(_sprite.texture);
                UnityEngine.Object.Destroy(_sprite);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowCard_DisplaysChineseDecisionContentWithoutInternalStableId()
        {
            var rig = CreateRig();
            var frame = UnityEngine.Object.Instantiate(rig.Template, _root.transform, false);
            frame.ShowCard(new CardViewModel(
                "lighting",
                _sprite,
                false,
                true,
                "雷击",
                "造成 10 点伤害",
                "目标：单位 | 范围：1 格 | 时间轴：2 格"));
            yield return null;

            Assert.That(frame.IsVisible, Is.True);
            Assert.That(frame.DisplayTitle, Is.EqualTo("雷击"));
            Assert.That(TextChild(frame, "Description").text, Is.EqualTo("造成 10 点伤害"));
            Assert.That(
                TextChild(frame, "Metadata").text,
                Is.EqualTo("目标：单位 | 范围：1 格 | 时间轴：2 格"));
            StringAssert.DoesNotContain("lighting", VisibleText(frame));
        }

        [UnityTest]
        public IEnumerator ShowAction_DistinguishesPlayerAndUnsupportedEnemyInChineseMetadata()
        {
            var rig = CreateRig();
            var frame = UnityEngine.Object.Instantiate(rig.Template, _root.transform, false);
            var player = CreateSnapshot(
                TimelineActorKind.Player,
                TimelineActionValidity.Valid,
                TimelineActionInvalidReason.None,
                "雷击",
                "造成 10 点伤害",
                null,
                "目标：目标 01");
            frame.ShowAction(player);

            Assert.That(frame.DisplayTitle, Is.EqualTo("玩家行动  雷击"));
            Assert.That(TextChild(frame, "Description").text, Is.EqualTo("造成 10 点伤害"));
            StringAssert.Contains("来源：玩家", TextChild(frame, "Metadata").text);
            StringAssert.Contains("目标：目标 01", TextChild(frame, "Metadata").text);
            StringAssert.Contains("占格：2", TextChild(frame, "Metadata").text);
            StringAssert.Contains("已排程", TextChild(frame, "Metadata").text);

            var enemy = CreateSnapshot(
                TimelineActorKind.Enemy,
                TimelineActionValidity.Unsupported,
                TimelineActionInvalidReason.UnsupportedSourceCommand,
                "意图",
                "源命令暂不支持，本轮不产生效果",
                "来源：村庄",
                "目标：目标 01");
            frame.ShowAction(enemy);

            Assert.That(frame.DisplayTitle, Is.EqualTo("敌方意图  意图"));
            Assert.That(
                TextChild(frame, "Description").text,
                Is.EqualTo("源命令暂不支持，本轮不产生效果"));
            StringAssert.Contains("来源：村庄", TextChild(frame, "Metadata").text);
            StringAssert.Contains("目标：目标 01", TextChild(frame, "Metadata").text);
            StringAssert.Contains("无效果 / 命令暂不支持", TextChild(frame, "Metadata").text);
            StringAssert.DoesNotContain("cycle:12/action:3", VisibleText(frame));

            frame.Hide();
            Assert.That(frame.IsVisible, Is.False);
            Assert.That(frame.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(frame.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(frame.GetComponent<CanvasGroup>().interactable, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Overlay_ReusesSingleFrameAndClearHidesIt()
        {
            var rig = CreateRig();
            rig.Overlay.ShowCard(new CardViewModel(
                "lighting",
                _sprite,
                false,
                true,
                "雷击",
                "造成 10 点伤害",
                "目标：单位 | 范围：1 格 | 时间轴：2 格"));
            var firstFrame = rig.Overlay.CurrentFrame;
            Assert.That(firstFrame, Is.Not.Null);
            Assert.That(rig.Host.GetComponentsInChildren<CardEffectFrame>(true), Has.Length.EqualTo(1));

            rig.Overlay.ShowAction(CreateSnapshot(
                TimelineActorKind.Enemy,
                TimelineActionValidity.Unsupported,
                TimelineActionInvalidReason.UnsupportedSourceCommand,
                "意图",
                "源命令暂不支持，本轮不产生效果",
                "来源：村庄",
                "目标：目标 01"));
            Assert.That(rig.Overlay.CurrentFrame, Is.SameAs(firstFrame));
            Assert.That(rig.Host.GetComponentsInChildren<CardEffectFrame>(true), Has.Length.EqualTo(1));
            Assert.That(firstFrame.DisplayTitle, Is.EqualTo("敌方意图  意图"));

            rig.Overlay.Clear();
            Assert.That(firstFrame.IsVisible, Is.False);
            yield return null;
        }

        private TooltipRig CreateRig()
        {
            _root = new GameObject("CardEffectFrameTestRig", typeof(RectTransform), typeof(Canvas));
            _root.SetActive(false);
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            _sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

            var host = CreateRect("EffectFrameHost", _root.transform);
            var template = CreateFrameTemplate(_root.transform);
            var overlayObject = new GameObject("CombatInteractionOverlayPresenter");
            overlayObject.transform.SetParent(_root.transform, false);
            var overlay = overlayObject.AddComponent<CombatInteractionOverlayPresenter>();
            SetField(overlay, "frameHost", host);
            SetField(overlay, "framePrefab", template);
            _root.SetActive(true);
            return new TooltipRig(template, overlay, host);
        }

        private static CardEffectFrame CreateFrameTemplate(Transform parent)
        {
            var frameObject = CreateRect("CardEffectFrameTemplate", parent).gameObject;
            var background = frameObject.AddComponent<Image>();
            background.raycastTarget = false;
            var canvasGroup = frameObject.AddComponent<CanvasGroup>();
            var frame = frameObject.AddComponent<CardEffectFrame>();
            var stripe = CreateRect("ActorStripe", frameObject.transform).gameObject.AddComponent<Image>();
            stripe.raycastTarget = false;
            var title = CreateRect("Title", frameObject.transform).gameObject.AddComponent<Text>();
            title.raycastTarget = false;
            var description = CreateRect("Description", frameObject.transform).gameObject.AddComponent<Text>();
            description.raycastTarget = false;
            var metadata = CreateRect("Metadata", frameObject.transform).gameObject.AddComponent<Text>();
            metadata.raycastTarget = false;
            SetField(frame, "background", background);
            SetField(frame, "actorStripe", stripe);
            SetField(frame, "title", title);
            SetField(frame, "description", description);
            SetField(frame, "metadata", metadata);
            SetField(frame, "canvasGroup", canvasGroup);
            return frame;
        }

        private static TimelineActionPresentationSnapshot CreateSnapshot(
            TimelineActorKind actorKind,
            TimelineActionValidity validity,
            TimelineActionInvalidReason invalidReason,
            string title,
            string description,
            string sourceLabel,
            string targetLabel)
        {
            return new TimelineActionPresentationSnapshot(
                new TimelineActionIdentity("cycle:12/action:3"),
                actorKind,
                0,
                actorKind == TimelineActorKind.Enemy ? "enemy-village-01" : null,
                actorKind == TimelineActorKind.Enemy ? new HexCoord(1, 0) : (HexCoord?)null,
                "target-01",
                new HexCoord(0, 0),
                actorKind == TimelineActorKind.Player ? "lighting" : null,
                actorKind == TimelineActorKind.Enemy ? "enemy-intent" : "Damage",
                new TimelineActionDisplayPayload(
                    title,
                    description,
                    actorKind == TimelineActorKind.Player ? "lighting" : "enemy-intent",
                    sourceLabel,
                    targetLabel),
                new TimelineCell(2, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) },
                new[] { new TimelineCell(2, 0), new TimelineCell(3, 0) },
                new[] { new HexCoord(0, 0) },
                validity,
                invalidReason,
                TimelineActionResolveState.Scheduled);
        }

        private static string VisibleText(CardEffectFrame frame)
        {
            var texts = frame.GetComponentsInChildren<Text>(true);
            return string.Join("\n", Array.ConvertAll(texts, text => text.text));
        }

        private static Text TextChild(Component root, string name)
        {
            var texts = root.GetComponentsInChildren<Text>(true);
            return Array.Find(texts, text => text.name == name);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing serialized field " + fieldName + ".");
            field.SetValue(target, value);
        }

        private sealed class TooltipRig
        {
            public TooltipRig(
                CardEffectFrame template,
                CombatInteractionOverlayPresenter overlay,
                RectTransform host)
            {
                Template = template;
                Overlay = overlay;
                Host = host;
            }

            public CardEffectFrame Template { get; }

            public CombatInteractionOverlayPresenter Overlay { get; }

            public RectTransform Host { get; }
        }
    }
}
