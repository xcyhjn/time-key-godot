using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Presentation;
using TimeKey.Presentation.Actions;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Cards;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.BattleFlow
{
    public sealed class BattleFlowPresenterTests
    {
        private GameObject _root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Apply_RefreshesDeckRoundAndTimecoinsInChineseWithSilver()
        {
            var rig = CreateRig();
            var hook = CreateFlow(initialEra: 3, initialPhase: 8, initialTimecoins: 12);
            StartFlow(hook);

            rig.Presenter.Apply(hook.Current);
            yield return null;

            Assert.That(rig.DrawPile.text, Is.EqualTo("牌库 7"));
            Assert.That(rig.Hand.text, Is.EqualTo("手牌 5"));
            Assert.That(rig.Discard.text, Is.EqualTo("弃牌 0"));
            Assert.That(rig.Round.text, Is.EqualTo("纪元 3  阶段 8/8"));
            Assert.That(rig.Timecoins.text, Is.EqualTo("时间币 12"));
            Assert.That(rig.Settlement.IsVisible, Is.False);
            Assert.That(rig.AllTexts.All(text => text.font == rig.Silver), Is.True);
            Assert.That(rig.AllTexts.All(text => text.font.name == "Silver"), Is.True);
        }

        [UnityTest]
        public IEnumerator Apply_IsIdempotentLocksInputAndRequestsRewardOnlyOnce()
        {
            var rig = CreateRig();
            var hook = CreateFlow();
            StartFlow(hook);
            var lockChanges = new List<bool>();
            rig.Presenter.InputLockChanged += lockChanges.Add;
            rig.Presenter.Apply(hook.Current);
            Assert.That(rig.CombatInput.interactable, Is.True);

            var resolution = hook.TryResolveOutcome(2, BattleOutcome.VictorySettlement);
            Assert.That(resolution.Succeeded, Is.True);
            rig.Presenter.Apply(hook.Current);
            rig.Presenter.Apply(hook.Current);

            Assert.That(lockChanges, Is.EqualTo(new[] { false, true }));
            Assert.That(rig.Presenter.IsInputLocked, Is.True);
            Assert.That(rig.CombatInput.interactable, Is.False);
            Assert.That(rig.Settlement.IsVisible, Is.True);
            Assert.That(rig.Settlement.IsRewardVisible, Is.True);
            Assert.That(rig.SettlementTitle.text, Is.EqualTo("战斗胜利"));
            Assert.That(rig.RewardLabel.text, Is.EqualTo("领取卡牌奖励"));

            var requested = new List<BattleRewardEntry>();
            rig.Settlement.RewardRequested += requested.Add;
            rig.RewardButton.onClick.Invoke();
            rig.RewardButton.onClick.Invoke();
            rig.Presenter.Apply(hook.Current);

            Assert.That(requested, Has.Count.EqualTo(1));
            Assert.That(requested[0].EntryStableId, Is.EqualTo("reward-entry"));
            Assert.That(rig.Settlement.IsRewardVisible, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Apply_VictoryAndDefeatRemainMutuallyExclusive()
        {
            var rig = CreateRig();
            var victory = CreateFlow();
            StartFlow(victory);
            Assert.That(
                victory.TryResolveOutcome(2, BattleOutcome.VictorySettlement).Succeeded,
                Is.True);
            rig.Presenter.Apply(victory.Current);
            Assert.That(rig.SettlementTitle.text, Is.EqualTo("战斗胜利"));
            Assert.That(rig.Settlement.IsRewardVisible, Is.True);

            var defeat = CreateFlow();
            StartFlow(defeat);
            Assert.That(defeat.TryResolveOutcome(2, BattleOutcome.Defeat).Succeeded, Is.True);
            rig.Presenter.Apply(defeat.Current);

            Assert.That(rig.SettlementTitle.text, Is.EqualTo("战斗失败"));
            Assert.That(rig.SettlementDetail.text, Is.EqualTo("本次战斗已经结束"));
            Assert.That(rig.Settlement.IsRewardVisible, Is.False);
            Assert.That(rig.CombatInput.interactable, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SavedActionFrame_SurvivesHandViewDestruction()
        {
            var rig = CreateRig();
            var hook = CreateFlow();
            StartFlow(hook);
            var action = CreateActionSnapshot();
            var remainingHand = hook.Current.Hand.Select(card => card.InstanceId).ToArray();
            Assert.That(
                hook.PrepareEndTurn(2, 1, remainingHand, new[] { action }).Succeeded,
                Is.True);
            Assert.That(
                hook.ExecutePrepared(2, TurnLifecycleRequestKind.EndTurn).Succeeded,
                Is.True);

            var actionChangeCount = 0;
            rig.Presenter.ActionDisplaySnapshotsChanged += _ => actionChangeCount++;
            rig.Presenter.Apply(hook.Current);
            rig.Presenter.Apply(hook.Current);
            Assert.That(actionChangeCount, Is.EqualTo(1));
            Assert.That(rig.Presenter.ActionDisplaySnapshots, Has.Count.EqualTo(1));

            var handCard = new GameObject(
                "DiscardedHandCard",
                typeof(RectTransform),
                typeof(CardHandView));
            handCard.transform.SetParent(_root.transform, false);
            UnityEngine.Object.Destroy(handCard);
            yield return null;
            Assert.That(handCard == null, Is.True);

            var frameRig = CreateActionFrameRig();
            var saved = rig.Presenter.ActionDisplaySnapshots[0];
            frameRig.Frame.Apply(saved, frameRig.Cells, frameRig.Layer);

            Assert.That(frameRig.Frame.ActionId, Is.EqualTo(action.ActionId));
            Assert.That(frameRig.Frame.Snapshot, Is.SameAs(saved));
            Assert.That(frameRig.Label.text, Is.EqualTo("雷击"));
            Assert.That(frameRig.Badge.text, Is.EqualTo("玩家"));
            Assert.That(saved.Display.Description, Is.EqualTo("造成 10 点伤害"));
            Assert.That(saved.Display.SourceLabel, Is.EqualTo("来源：玩家"));
            Assert.That(saved.Display.TargetLabel, Is.EqualTo("目标：村庄"));
        }

        private BattleFlowRig CreateRig()
        {
            _root = new GameObject("BattleFlowPresentationTests", typeof(RectTransform), typeof(Canvas));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var silver = Resources.Load<Font>("Fonts/Silver");
            Assert.That(silver, Is.Not.Null);

            var draw = CreateText("DrawPile", _root.transform);
            var hand = CreateText("Hand", _root.transform);
            var discard = CreateText("DiscardPile", _root.transform);
            var round = CreateText("Round", _root.transform);
            var timecoins = CreateText("Timecoins", _root.transform);
            var combatInput = CreateButton("CombatInput", _root.transform, out _);

            var settlementRoot = CreateRect("Settlement", _root.transform).gameObject;
            var settlementCanvas = settlementRoot.AddComponent<CanvasGroup>();
            var settlementTitle = CreateText("SettlementTitle", settlementRoot.transform);
            var settlementDetail = CreateText("SettlementDetail", settlementRoot.transform);
            var rewardButton = CreateButton(
                "RewardButton",
                settlementRoot.transform,
                out var rewardLabel);
            var settlement = settlementRoot.AddComponent<BattleSettlementPresenter>();
            SetField(settlement, "rootCanvasGroup", settlementCanvas);
            SetField(settlement, "title", settlementTitle);
            SetField(settlement, "detail", settlementDetail);
            SetField(settlement, "rewardButton", rewardButton);
            SetField(settlement, "rewardButtonLabel", rewardLabel);
            SetField(settlement, "silverFont", silver);

            var presenter = _root.AddComponent<BattleFlowPresenter>();
            SetField(presenter, "drawPileCount", draw);
            SetField(presenter, "handCount", hand);
            SetField(presenter, "discardPileCount", discard);
            SetField(presenter, "roundLabel", round);
            SetField(presenter, "timecoinsLabel", timecoins);
            SetField(presenter, "settlementPresenter", settlement);
            SetField(presenter, "combatInputControls", new Selectable[] { combatInput });
            SetField(presenter, "silverFont", silver);

            return new BattleFlowRig(
                presenter,
                settlement,
                draw,
                hand,
                discard,
                round,
                timecoins,
                combatInput,
                settlementTitle,
                settlementDetail,
                rewardButton,
                rewardLabel,
                silver,
                new[]
                {
                    draw,
                    hand,
                    discard,
                    round,
                    timecoins,
                    settlementTitle,
                    settlementDetail,
                    rewardLabel
                });
        }

        private ActionFrameRig CreateActionFrameRig()
        {
            var layer = CreateRect("ActionLayer", _root.transform);
            layer.sizeDelta = new Vector2(240f, 80f);
            var cellObject = CreateRect("TimelineCell", layer).gameObject;
            var cellImage = cellObject.AddComponent<Image>();
            var cellButton = cellObject.AddComponent<Button>();
            cellButton.targetGraphic = cellImage;
            var cellLabel = CreateText("CellLabel", cellObject.transform);
            var cell = cellObject.AddComponent<TimelineCellView>();
            SetField(cell, "column", 0);
            SetField(cell, "row", 0);
            SetField(cell, "button", cellButton);
            SetField(cell, "label", cellLabel);
            ((RectTransform)cell.transform).sizeDelta = new Vector2(72f, 46f);

            var frameObject = CreateRect("ActionFrame", layer).gameObject;
            var background = frameObject.AddComponent<Image>();
            var canvasGroup = frameObject.AddComponent<CanvasGroup>();
            var outline = frameObject.AddComponent<Outline>();
            var stripe = CreateRect("Stripe", frameObject.transform).gameObject.AddComponent<Image>();
            var label = CreateText("Label", frameObject.transform);
            var badge = CreateText("Badge", frameObject.transform);
            var frame = frameObject.AddComponent<TimelineActionFrame>();
            SetField(frame, "background", background);
            SetField(frame, "stripe", stripe);
            SetField(frame, "outline", outline);
            SetField(frame, "label", label);
            SetField(frame, "badge", badge);
            SetField(frame, "canvasGroup", canvasGroup);

            return new ActionFrameRig(
                frame,
                layer,
                new Dictionary<TimelineCell, TimelineCellView>
                {
                    { new TimelineCell(0, 0), cell }
                },
                label,
                badge);
        }

        private static BattleFlowNextTurnHook CreateFlow(
            int initialEra = 1,
            int initialPhase = 1,
            int initialTimecoins = 0)
        {
            return new BattleFlowNextTurnHook(
                DeckState.CreateStarter(731UL, "battle-flow-presentation"),
                new BattleRoundLedger(initialEra, initialPhase, initialTimecoins),
                new BattleSettlementState(
                    "presentation-test",
                    731,
                    new BattleRewardEntry(
                        "reward-entry",
                        BattleRewardKind.Acquire,
                        "领取卡牌奖励")));
        }

        private static void StartFlow(BattleFlowNextTurnHook hook)
        {
            Assert.That(hook.PrepareInitialStart(1).Succeeded, Is.True);
            Assert.That(
                hook.ExecutePrepared(1, TurnLifecycleRequestKind.InitialStart).Succeeded,
                Is.True);
        }

        private static TimelineActionPresentationSnapshot CreateActionSnapshot()
        {
            return new TimelineActionPresentationSnapshot(
                new TimelineActionIdentity("cycle:2/action:0"),
                TimelineActorKind.Player,
                0,
                null,
                null,
                "village-01",
                new HexCoord(0, 0),
                "lighting",
                "Damage",
                new TimelineActionDisplayPayload(
                    "雷击",
                    "造成 10 点伤害",
                    "lighting",
                    "来源：玩家",
                    "目标：村庄"),
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0) },
                new[] { new TimelineCell(0, 0) },
                new[] { new HexCoord(0, 0) },
                TimelineActionValidity.Valid,
                TimelineActionInvalidReason.None,
                TimelineActionResolveState.Scheduled);
        }

        private static Text CreateText(string name, Transform parent)
        {
            var text = CreateRect(name, parent).gameObject.AddComponent<Text>();
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, out Text label)
        {
            var buttonObject = CreateRect(name, parent).gameObject;
            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = CreateText("Label", buttonObject.transform);
            return button;
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

        private sealed class BattleFlowRig
        {
            public BattleFlowRig(
                BattleFlowPresenter presenter,
                BattleSettlementPresenter settlement,
                Text drawPile,
                Text hand,
                Text discard,
                Text round,
                Text timecoins,
                Button combatInput,
                Text settlementTitle,
                Text settlementDetail,
                Button rewardButton,
                Text rewardLabel,
                Font silver,
                IReadOnlyList<Text> allTexts)
            {
                Presenter = presenter;
                Settlement = settlement;
                DrawPile = drawPile;
                Hand = hand;
                Discard = discard;
                Round = round;
                Timecoins = timecoins;
                CombatInput = combatInput;
                SettlementTitle = settlementTitle;
                SettlementDetail = settlementDetail;
                RewardButton = rewardButton;
                RewardLabel = rewardLabel;
                Silver = silver;
                AllTexts = allTexts;
            }

            public BattleFlowPresenter Presenter { get; }
            public BattleSettlementPresenter Settlement { get; }
            public Text DrawPile { get; }
            public Text Hand { get; }
            public Text Discard { get; }
            public Text Round { get; }
            public Text Timecoins { get; }
            public Button CombatInput { get; }
            public Text SettlementTitle { get; }
            public Text SettlementDetail { get; }
            public Button RewardButton { get; }
            public Text RewardLabel { get; }
            public Font Silver { get; }
            public IReadOnlyList<Text> AllTexts { get; }
        }

        private sealed class ActionFrameRig
        {
            public ActionFrameRig(
                TimelineActionFrame frame,
                RectTransform layer,
                IReadOnlyDictionary<TimelineCell, TimelineCellView> cells,
                Text label,
                Text badge)
            {
                Frame = frame;
                Layer = layer;
                Cells = cells;
                Label = label;
                Badge = badge;
            }

            public TimelineActionFrame Frame { get; }
            public RectTransform Layer { get; }
            public IReadOnlyDictionary<TimelineCell, TimelineCellView> Cells { get; }
            public Text Label { get; }
            public Text Badge { get; }
        }
    }
}
