using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.OutOfBattleShell;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.OutOfBattleShell
{
    public sealed class CombatShellGateDVisualEvidenceTests
    {
        private const float TimeoutSeconds = 15f;
        private static readonly Vector2Int[] Viewports =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1080)
        };

        private GameObject _eventSystem;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_eventSystem != null)
            {
                Object.Destroy(_eventSystem);
            }

            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator FormalScenes_RenderRoomStatesAndGameOverAtThreeViewports()
        {
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);
            _eventSystem = new GameObject(
                "GateDVisualEventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            var eventSystem = _eventSystem.GetComponent<EventSystem>();
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left
            };

            var start = StartPayload();
            yield return LoadSingle("OutOfBattleShell");
            var entry = Object.FindFirstObjectByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            entry.Bind(start);
            entry.SetCameraEnabled(true);
            var camera = Object.FindFirstObjectByType<Camera>();
            var presenter = Object.FindFirstObjectByType<OutOfBattleShellPresenter>();
            var navigation = Object.FindFirstObjectByType<OutOfBattleShellSceneNavigation>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(navigation, Is.Not.Null);
            PrepareCanvas(camera);
            navigation.enabled = false;
            presenter.Apply(new OutOfBattleShellState(start));
            AssertSilver(presenter.gameObject);

            yield return CaptureViewports(camera, evidence, "out-of-battle", "idle");
            var room = FindButton(presenter.gameObject, "CombatRoom");
            ExecuteEvents.Execute(
                room.gameObject,
                pointer,
                ExecuteEvents.pointerEnterHandler);
            yield return CaptureViewports(camera, evidence, "out-of-battle", "hover");
            ExecuteEvents.Execute(
                room.gameObject,
                pointer,
                ExecuteEvents.pointerExitHandler);
            room.onClick.Invoke();
            Assert.That(presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Selected));
            yield return CaptureViewports(camera, evidence, "out-of-battle", "selected");
            FindButton(presenter.gameObject, "ConfirmButton").onClick.Invoke();
            Assert.That(presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Confirming));
            yield return CaptureViewports(camera, evidence, "out-of-battle", "confirming");

            var settled = SettledState(start);
            presenter.Apply(settled.State);
            Assert.That(presenter.RoomState, Is.EqualTo(OutOfBattleRoomState.Settled));
            yield return CaptureViewports(camera, evidence, "out-of-battle", "settled");

            yield return LoadSingle("GameOver");
            entry = Object.FindFirstObjectByType<SceneContentEntry>(FindObjectsInactive.Include);
            entry.Bind(settled.DefeatOutcome);
            entry.SetCameraEnabled(true);
            camera = Object.FindFirstObjectByType<Camera>();
            var gameOver = Object.FindFirstObjectByType<GameOverPresenter>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(gameOver, Is.Not.Null);
            PrepareCanvas(camera);
            gameOver.Apply(settled.DefeatOutcome);
            AssertSilver(gameOver.gameObject);
            yield return CaptureViewports(camera, evidence, "game-over", "defeat");
        }

        private static RunStartPayload StartPayload()
        {
            return new RunStartPayload(
                RunStartKind.SeedGame,
                "gate-d-visual-run",
                731,
                "731",
                1,
                2,
                3,
                64,
                "silver-character",
                StarterDeck.OrderedStableIds);
        }

        private static SettledFixture SettledState(RunStartPayload start)
        {
            var state = new OutOfBattleShellState(start);
            var launch = state.CreateCombatLaunch(
                "gate-d-visual-launch",
                "combat-room-01",
                "combat-vertical-slice",
                731);
            Assert.That(state.TryBeginCombat(launch), Is.EqualTo(CombatLaunchApplyFailure.None));

            var victorySettlement = Settlement(launch);
            victorySettlement.TryResolve(1, BattleOutcome.VictorySettlement);
            victorySettlement.TryClaimReward(2);
            var victoryBoundary = victorySettlement.TryCreateReturnBoundary(
                new BattleRoundLedger(2, 4, 80).Snapshot,
                launch.DeckStableIds);
            var victory = CombatOutcome.TryCreate(
                "gate-d-visual-victory",
                launch,
                victorySettlement.Snapshot,
                victoryBoundary.Payload).Outcome;
            Assert.That(state.TryApplyOutcome(victory).Succeeded, Is.True);

            var defeatSettlement = Settlement(launch);
            defeatSettlement.TryResolve(1, BattleOutcome.Defeat);
            var defeatBoundary = defeatSettlement.TryCreateReturnBoundary(
                new BattleRoundLedger(2, 3, 64).Snapshot,
                launch.DeckStableIds);
            var defeat = CombatOutcome.TryCreate(
                "gate-d-visual-defeat",
                launch,
                defeatSettlement.Snapshot,
                defeatBoundary.Payload).Outcome;
            return new SettledFixture(state, defeat);
        }

        private static BattleSettlementState Settlement(CombatLaunchPayload launch)
        {
            return new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "gate-d-visual-reward",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
        }

        private static IEnumerator LoadSingle(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!operation.isDone && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(operation.isDone, Is.True, sceneName + " load timed out.");
            yield return null;
        }

        private static Button FindButton(GameObject root, string name)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("Missing button: " + name);
            return null;
        }

        private static void AssertSilver(GameObject root)
        {
            var silver = Resources.Load<Font>("Fonts/Silver");
            Assert.That(silver, Is.Not.Null);
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.EqualTo(silver), text.name);
            }
        }

        private static IEnumerator CaptureViewports(
            Camera camera,
            string directory,
            string scene,
            string state)
        {
            foreach (var viewport in Viewports)
            {
                yield return Capture(
                    camera,
                    directory,
                    scene + "-" + viewport.x + "x" + viewport.y + "-" + state + ".png",
                    viewport.x,
                    viewport.y);
            }
        }

        private static IEnumerator Capture(
            Camera camera,
            string directory,
            string fileName,
            int width,
            int height)
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var priorTarget = camera.targetTexture;
            var priorActive = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            var path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Assert.That(HasVisibleVariation(texture), Is.True, path);
            Object.Destroy(texture);
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            target.Release();
            Object.Destroy(target);
        }

        private static bool HasVisibleVariation(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var first = pixels[0];
            var step = Math.Max(1, pixels.Length / 2048);
            for (var index = step; index < pixels.Length; index += step)
            {
                if (!pixels[index].Equals(first))
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrepareCanvas(Camera camera)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
        }

        private static string EvidenceDirectory()
        {
            var root = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            Assert.That(root, Is.Not.Null.And.Not.Empty);
            return Path.Combine(
                root,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "combat-shell-gate-d");
        }

        private sealed class SettledFixture
        {
            public SettledFixture(OutOfBattleShellState state, CombatOutcome defeatOutcome)
            {
                State = state;
                DefeatOutcome = defeatOutcome;
            }

            public OutOfBattleShellState State { get; }

            public CombatOutcome DefeatOutcome { get; }
        }
    }
}
