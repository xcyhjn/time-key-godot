using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation.OutOfBattleShell;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.OutOfBattleShell
{
    public sealed class OutOfBattleShellPresenterTests
    {
        private const string RoomId = "room-gate-d";
        private static readonly string[] Deck = { "card-a", "card-b", "card-c" };

        private GameObject _root;
        private GameObject _eventSystemRoot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneInputLockState.SetLocked(false);
            if (EventSystem.current == null)
            {
                _eventSystemRoot = new GameObject("EventSystem", typeof(EventSystem));
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneInputLockState.SetLocked(false);
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            if (_eventSystemRoot != null)
            {
                Object.Destroy(_eventSystemRoot);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Apply_ProjectsSnapshotTextAndSilverFont()
        {
            var rig = CreateRig();
            rig.Presenter.Apply(State(settled: false));

            Assert.That(rig.Seed.text, Is.EqualTo("种子 731"));
            Assert.That(rig.Era.text, Is.EqualTo("时代 2"));
            Assert.That(rig.Phase.text, Is.EqualTo("阶段 3"));
            Assert.That(rig.Timecoins.text, Is.EqualTo("时间币 17"));
            Assert.That(rig.RoomTitle.text, Is.EqualTo("废墟战场"));
            Assert.That(rig.RoomStatus.text, Is.EqualTo("可进入"));
            Assert.That(rig.Presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Idle));
            foreach (var text in _root.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.SameAs(rig.Silver));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Room_ShowsHoverFocusSelectionAndEscapeReturnsIdle()
        {
            var rig = CreateRig();
            rig.Presenter.Apply(State(settled: false));
            var pointer = new PointerEventData(EventSystem.current);
            var focus = new BaseEventData(EventSystem.current);

            rig.Room.OnPointerEnter(pointer);
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Hovered));
            rig.Room.OnPointerExit(pointer);
            Assert.That(rig.Presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Idle));
            rig.Room.OnSelect(focus);
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Hovered));
            rig.Room.OnDeselect(focus);
            Assert.That(rig.Presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Idle));

            rig.RoomButton.onClick.Invoke();
            Assert.That(rig.Presenter.IsSelected, Is.True);
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Selected));
            Assert.That(rig.Confirm.interactable, Is.True);
            Assert.That(rig.Presenter.HandleEscape(), Is.True);
            Assert.That(rig.Presenter.IsSelected, Is.False);
            Assert.That(rig.Presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Idle));
            Assert.That(rig.Confirm.interactable, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Confirm_PublishesStableRoomOnceAndHonorsBothLocks()
        {
            var rig = CreateRig();
            rig.Presenter.Apply(State(settled: false));
            var requests = new List<OutOfBattleRoomConfirmationRequest>();
            rig.Presenter.RoomConfirmationRequested += requests.Add;

            rig.RoomButton.onClick.Invoke();
            rig.Confirm.onClick.Invoke();
            rig.Confirm.onClick.Invoke();
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.That(requests[0].RoomId, Is.EqualTo(RoomId));
            Assert.That(rig.Presenter.IsConfirmationPending, Is.True);
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Confirming));

            Assert.That(rig.Presenter.RejectPendingConfirmation(), Is.True);
            SceneInputLockState.SetLocked(true);
            rig.Presenter.RefreshInputState();
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Disabled));
            rig.Confirm.onClick.Invoke();
            Assert.That(requests, Has.Count.EqualTo(1));

            SceneInputLockState.SetLocked(false);
            rig.Presenter.SetTransitionLocked(true);
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Disabled));
            rig.Confirm.onClick.Invoke();
            Assert.That(requests, Has.Count.EqualTo(1));
            rig.Presenter.SetTransitionLocked(false);
            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Selected));
            rig.Confirm.onClick.Invoke();
            Assert.That(requests, Has.Count.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettledRoom_IsDisabledAndCannotPublishConfirmation()
        {
            var rig = CreateRig();
            rig.Presenter.Apply(State(settled: true));
            var requests = new List<OutOfBattleRoomConfirmationRequest>();
            rig.Presenter.RoomConfirmationRequested += requests.Add;

            Assert.That(rig.Presenter.RoomState,
                Is.EqualTo(OutOfBattleRoomState.Settled));
            Assert.That(rig.RoomStatus.text, Is.EqualTo("已完成"));
            Assert.That(rig.RoomButton.interactable, Is.False);
            Assert.That(rig.Confirm.interactable, Is.False);
            rig.RoomButton.onClick.Invoke();
            rig.Confirm.onClick.Invoke();
            Assert.That(rig.Presenter.IsSelected, Is.False);
            Assert.That(requests, Is.Empty);
            yield return null;
        }

        private Rig CreateRig()
        {
            _root = new GameObject("OutOfBattleShellRig");
            _root.SetActive(false);
            var presenter = _root.AddComponent<OutOfBattleShellPresenter>();
            var seed = CreateText("Seed", _root.transform);
            var era = CreateText("Era", _root.transform);
            var phase = CreateText("Phase", _root.transform);
            var timecoins = CreateText("Timecoins", _root.transform);

            var roomObject = new GameObject(
                "Room",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            roomObject.transform.SetParent(_root.transform, false);
            var roomBackground = roomObject.GetComponent<Image>();
            var roomButton = roomObject.GetComponent<Button>();
            roomButton.targetGraphic = roomBackground;
            var roomTitle = CreateText("RoomTitle", roomObject.transform);
            var roomStatus = CreateText("RoomStatus", roomObject.transform);
            var room = roomObject.AddComponent<OutOfBattleRoomView>();
            SetField(room, "roomButton", roomButton);
            SetField(room, "background", roomBackground);
            SetField(room, "titleLabel", roomTitle);
            SetField(room, "statusLabel", roomStatus);

            var confirm = CreateButton("Confirm", _root.transform, "进入战斗");
            var cancel = CreateButton("Cancel", _root.transform, "取消选择");
            var silver = Resources.Load<Font>("Fonts/Silver");
            SetField(presenter, "seedLabel", seed);
            SetField(presenter, "eraLabel", era);
            SetField(presenter, "phaseLabel", phase);
            SetField(presenter, "timecoinsLabel", timecoins);
            SetField(presenter, "roomView", room);
            SetField(presenter, "confirmButton", confirm);
            SetField(presenter, "cancelButton", cancel);
            SetField(presenter, "silverFont", silver);
            SetField(presenter, "roomId", RoomId);
            SetField(presenter, "roomTitle", "废墟战场");
            _root.SetActive(true);
            return new Rig(
                presenter,
                room,
                roomButton,
                confirm,
                cancel,
                seed,
                era,
                phase,
                timecoins,
                roomTitle,
                roomStatus,
                silver);
        }

        private Text CreateText(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.AddComponent<Text>();
        }

        private Button CreateButton(string name, Transform parent, string label)
        {
            var target = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            target.transform.SetParent(parent, false);
            var button = target.GetComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();
            CreateText(name + "Label", target.transform).text = label;
            return button;
        }

        private static OutOfBattleShellState State(bool settled)
        {
            var start = new RunStartPayload(
                RunStartKind.SeedGame,
                "run-gate-d",
                731,
                "731",
                1,
                2,
                3,
                17,
                Deck);
            var state = new OutOfBattleShellState(start);
            if (!settled)
            {
                return state;
            }

            var launch = new CombatLaunchPayload(
                "launch-gate-d",
                start.RunId,
                start.RunSeed,
                start.Chapter,
                start.Era,
                start.Phase,
                RoomId,
                "character-silver",
                start.Timecoins,
                "gate-d-battle",
                1731,
                Deck);
            if (state.TryBeginCombat(launch) != CombatLaunchApplyFailure.None)
            {
                throw new AssertionException("The settled fixture launch was rejected.");
            }

            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "reward-gate-d",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            settlement.TryClaimReward(2);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(start.Era, start.Phase, start.Timecoins).Snapshot,
                Deck);
            var outcome = CombatOutcome.TryCreate(
                "outcome-gate-d",
                launch,
                settlement.Snapshot,
                boundary.Payload).Outcome;
            if (!state.TryApplyOutcome(outcome).Succeeded)
            {
                throw new AssertionException("The settled fixture outcome was rejected.");
            }

            return state;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private sealed class Rig
        {
            public Rig(
                OutOfBattleShellPresenter presenter,
                OutOfBattleRoomView room,
                Button roomButton,
                Button confirm,
                Button cancel,
                Text seed,
                Text era,
                Text phase,
                Text timecoins,
                Text roomTitle,
                Text roomStatus,
                Font silver)
            {
                Presenter = presenter;
                Room = room;
                RoomButton = roomButton;
                Confirm = confirm;
                Cancel = cancel;
                Seed = seed;
                Era = era;
                Phase = phase;
                Timecoins = timecoins;
                RoomTitle = roomTitle;
                RoomStatus = roomStatus;
                Silver = silver;
            }

            public OutOfBattleShellPresenter Presenter { get; }
            public OutOfBattleRoomView Room { get; }
            public Button RoomButton { get; }
            public Button Confirm { get; }
            public Button Cancel { get; }
            public Text Seed { get; }
            public Text Era { get; }
            public Text Phase { get; }
            public Text Timecoins { get; }
            public Text RoomTitle { get; }
            public Text RoomStatus { get; }
            public Font Silver { get; }
        }
    }
}
