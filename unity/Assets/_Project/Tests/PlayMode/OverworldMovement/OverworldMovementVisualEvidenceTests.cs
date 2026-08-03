using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.Deck;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Infrastructure.Persistence;
using TimeKey.Presentation.MainMenu;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.OverworldMovement;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.OverworldMovement
{
    public sealed class OverworldMovementVisualEvidenceTests
    {
        private const float TimeoutSeconds = 20f;

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
                _eventSystem = null;
            }

            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator GateC_DynamicMapCapturesThreeViewportsResizeStatesEventAndShop()
        {
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-map-visual-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            var seed = FindSeedWithAllRoomTypes();
            var start = Start("gate-c-visual-run", seed, 100);
            yield return LoadSingle("Bootstrap");
            var bootstrap = Object.FindFirstObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            var menuSequence = bootstrap.ReserveTransitionSequence();
            var menuTransition = bootstrap.TransitionAsync(new SceneTransitionRequest(
                menuSequence,
                "gate-c-visual-main-menu",
                SceneId.GameStart,
                SceneId.MainMenu,
                new EmptySceneTransitionPayload(SceneId.MainMenu)));
            yield return AwaitTask(menuTransition, "MainMenu transition");
            Assert.That(menuTransition.Result.Succeeded, Is.True, menuTransition.Result.Message);
            var store = Object.FindFirstObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var shellSequence = bootstrap.ReserveTransitionSequence();
            var shellTransition = bootstrap.TransitionAsync(new SceneTransitionRequest(
                shellSequence,
                "gate-c-visual-overworld",
                SceneId.MainMenu,
                SceneId.OutOfBattleShell,
                start));
            yield return AwaitTask(shellTransition, "OutOfBattleShell transition");
            Assert.That(shellTransition.Result.Succeeded, Is.True, shellTransition.Result.Message);
            yield return AwaitInputUnlock();
            yield return AwaitRevealComplete();
            yield return new WaitForSecondsRealtime(0.4f);

            var run = store.OverworldRun;
            Assert.That(run, Is.Not.Null);
            yield return null;
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var camera = Object.FindFirstObjectByType<Camera>();
            var presenter = Object.FindFirstObjectByType<OutOfBattleShellPresenter>();
            var movement = Object.FindFirstObjectByType<OverworldMovementPresenter>();
            var input = Object.FindFirstObjectByType<OverworldMapInputController>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(movement, Is.Not.Null);
            Assert.That(input, Is.Not.Null);

            PrepareCanvas(camera);

            var views = movement.GetComponentsInChildren<OverworldNodeView>(true);
            Assert.That(views.Length, Is.EqualTo(run.Map.Nodes.Count));
            Assert.That(views.Select(view => view.Id.Value),
                Is.EquivalentTo(run.Map.Nodes.Select(node => node.Id.Value)));
            Assert.That(views.Count(view => view.RoomType == OverworldRoomType.Boss),
                Is.EqualTo(1));
            Assert.That(AllVisibleTextUsesSilver(), Is.True);
            yield return CaptureViewports(camera, evidence, "map-overview");

            Screen.SetResolution(1600, 900, false);
            yield return null;
            yield return null;
            AssertStableShellBounds(camera, 1600, 900);
            yield return Capture(camera, evidence, "dynamic-resize-1600x900.png", 1600, 900);

            var current = views.Single(view =>
                view.Id == run.ChapterSnapshot.CurrentNodeId);
            Assert.That(movement.TryMove(current.Id), Is.False);
            Assert.That(FindText("MovementStatus").text, Does.Contain("无需移动"));

            var target = views.First(view =>
                view.State == OverworldNodeVisualState.Available &&
                view.RoomType != OverworldRoomType.Event &&
                view.RoomType != OverworldRoomType.Shop);
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = Vector2.zero
            };
            input.OnBeginDrag(pointer);
            pointer.position = new Vector2(40f, 0f);
            input.OnDrag(pointer);
            input.OnEndDrag(pointer);
            target.GetComponent<Button>().onClick.Invoke();
            Assert.That(movement.SelectedNodeId, Is.Empty);
            input.BeginPointerGesture();

            target.OnPointerEnter(pointer);
            target.OnSelect(new BaseEventData(eventSystem));
            yield return Capture(camera, evidence, "hover-focus-1280x720.png", 1280, 720);
            Assert.That(movement.TryMove(target.Id), Is.True);
            yield return new WaitForSecondsRealtime(0.14f);
            yield return Capture(camera, evidence, "moving-1920x1080.png", 1920, 1080);
            yield return AwaitMovement(movement);
            Assert.That(movement.SelectedNodeId, Is.EqualTo(target.Id.Value));
            yield return CaptureViewports(camera, evidence, "selected");

            movement.SetRoomInteractionState(target.Id, selected: true, confirming: true);
            Assert.That(target.State, Is.EqualTo(OverworldNodeVisualState.Confirming));
            yield return Capture(camera, evidence, "confirming-1280x720.png", 1280, 720);
            movement.SetRoomInteractionState(target.Id, selected: false, confirming: false);
            Assert.That(movement.SelectedNodeId, Is.Empty);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(target.gameObject));

            ApplyAllNodeStates(views);
            yield return Capture(camera, evidence, "all-node-states-1920x1080.png", 1920, 1080);

            var eventNode = views.First(view => view.RoomType == OverworldRoomType.Event);
            presenter.SetCurrentRoomPresentation(
                eventNode.Id,
                "时序事件 · " + eventNode.Id.Value,
                "Godot 的事件场景资源缺失。可安全跳过此节点，不会生成剧情或奖励。",
                "跳过事件",
                confirmAvailable: true);
            FindButton("CombatRoom").onClick.Invoke();
            yield return null;
            yield return Capture(camera, evidence, "event-safe-skip-1920x1080.png", 1920, 1080);

            presenter.Apply(new OutOfBattleShellState(start));
            var shopNode = views.First(view => view.RoomType == OverworldRoomType.Shop);
            var offer = run.GetShopOffer(shopNode.Id);
            presenter.SetCurrentRoomPresentation(
                shopNode.Id,
                "时序商店 · " + shopNode.Id.Value,
                "商品：" + CardName(offer.CardStableId) +
                "\n价格：" + offer.TimecoinCost + " 时间币\n余额：100",
                "购买并离开",
                confirmAvailable: true);
            FindButton("CombatRoom").onClick.Invoke();
            yield return null;
            yield return Capture(camera, evidence, "shop-offer-1920x1080.png", 1920, 1080);

            Assert.That(AllVisibleTextUsesSilver(), Is.True);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator GateC_ContinueAndInvalidSaveCaptureRecoverableMenuStates()
        {
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-continue-visual-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            var run = new OverworldRunApplication(
                Start("gate-c-continue-visual-run", 731, 100),
                new OverworldMapGenerationConfig(1),
                finalChapter: 3);
            Assert.That(new OverworldSaveRepository(savePath).Save(
                OverworldSaveMapper.ToDocument(run.CreatePersistenceSnapshot())).Succeeded,
                Is.True);

            yield return LoadBootstrapMainMenu(savePath);
            var camera = Object.FindFirstObjectByType<Camera>();
            PrepareCanvas(camera);
            var menu = Object.FindFirstObjectByType<MainMenuPresenter>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(FindButton("Continue").interactable, Is.True);
            Assert.That(menu.IsModalOpen, Is.False);
            yield return Capture(camera, evidence, "continue-valid-1920x1080.png", 1920, 1080);

            File.WriteAllText(savePath, "{not-json");
            yield return LoadBootstrapMainMenu(savePath);
            camera = Object.FindFirstObjectByType<Camera>();
            PrepareCanvas(camera);
            menu = Object.FindFirstObjectByType<MainMenuPresenter>();
            var navigation = Object.FindFirstObjectByType<MainMenuSceneNavigation>();
            Assert.That(FindButton("Continue").interactable, Is.False);
            Assert.That(menu.IsModalOpen, Is.True);
            Assert.That(navigation.LastContinueNotice, Does.Contain("损坏"));
            Assert.That(FindText("ModalBody").text, Does.Contain("旧文件仍保留"));
            yield return Capture(camera, evidence, "continue-corrupt-1920x1080.png", 1920, 1080);

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        private static void ApplyAllNodeStates(IReadOnlyList<OverworldNodeView> views)
        {
            var states = new[]
            {
                OverworldNodeVisualState.Current,
                OverworldNodeVisualState.Available,
                OverworldNodeVisualState.Locked,
                OverworldNodeVisualState.Visited,
                OverworldNodeVisualState.Settled,
                OverworldNodeVisualState.Selected,
                OverworldNodeVisualState.Confirming
            };
            for (var index = 0; index < views.Count; index++)
            {
                views[index].SetVisualState(states[index % states.Length]);
            }
        }

        private static IEnumerator LoadBootstrapMainMenu(string savePath)
        {
            yield return LoadSingle("Bootstrap");
            var bootstrap = Object.FindFirstObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            Object.FindFirstObjectByType<SceneFlowStateStore>()
                .ConfigurePersistencePath(savePath);
            var sequence = bootstrap.ReserveTransitionSequence();
            var transition = bootstrap.TransitionAsync(new SceneTransitionRequest(
                sequence,
                "gate-c-continue-menu-" + sequence,
                SceneId.GameStart,
                SceneId.MainMenu,
                new EmptySceneTransitionPayload(SceneId.MainMenu)));
            yield return AwaitTask(transition, "MainMenu transition");
            Assert.That(transition.Result.Succeeded, Is.True, transition.Result.Message);
            yield return null;
        }

        private static IEnumerator AwaitInputUnlock()
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (SceneInputLockState.IsLocked)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Scene reveal did not release input.");
                }

                yield return null;
            }
        }

        private static IEnumerator AwaitRevealComplete()
        {
            var reveal = Object.FindFirstObjectByType<LayeredSceneRevealPresenter>();
            Assert.That(reveal, Is.Not.Null);
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!reveal.IsComplete)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Out-of-battle reveal did not complete.");
                }

                yield return null;
            }
        }

        private static IEnumerator AwaitMovement(OverworldMovementPresenter movement)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (movement.IsMoving)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Dynamic map selection timed out.");
                }

                yield return null;
            }
        }

        private static IEnumerator AwaitTask(Task task, string operation)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(operation + " timed out.");
                }

                yield return null;
            }

            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.IsCanceled, Is.False);
        }

        private static int FindSeedWithAllRoomTypes()
        {
            var generator = new DeterministicOverworldMapGenerator();
            for (var seed = 1; seed <= 10000; seed++)
            {
                var map = generator.Generate(
                    seed,
                    new OverworldMapGenerationConfig(1));
                var types = new HashSet<OverworldRoomType>(
                    map.Nodes.Select(node => node.RoomType));
                if (map.Nodes.Count == 14 &&
                    types.Contains(OverworldRoomType.Event) &&
                    types.Contains(OverworldRoomType.Shop) &&
                    types.Contains(OverworldRoomType.Battle) &&
                    types.Contains(OverworldRoomType.Boss))
                {
                    return seed;
                }
            }

            Assert.Fail("No deterministic map seed contains all Gate C room types.");
            return 0;
        }

        private static RunStartPayload Start(string runId, int seed, int timecoins)
        {
            return new RunStartPayload(
                RunStartKind.SeedGame,
                runId,
                seed,
                seed.ToString(),
                1,
                2,
                3,
                timecoins,
                "silver-character",
                StarterDeck.OrderedStableIds);
        }

        private static string CardName(string stableId)
        {
            switch (stableId)
            {
                case "lighting": return "雷击";
                case "earthquake": return "地震";
                case "recover": return "复苏";
                case "heavy_rain": return "暴雨";
                case "sandstorm": return "沙暴";
                case "wind": return "疾风";
                case "freeze": return "冰封";
                default: return stableId;
            }
        }

        private static Button FindButton(string name)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .First(button => button.gameObject.name == name);
        }

        private static Text FindText(string name)
        {
            return Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                .First(text => text.gameObject.name == name);
        }

        private static bool AllVisibleTextUsesSilver()
        {
            return Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                .Where(text => text.gameObject.activeInHierarchy &&
                    text.gameObject.scene.name == "OutOfBattleShell")
                .All(text => text.font != null &&
                    text.font.name.IndexOf("silver", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IEnumerator LoadSingle(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static IEnumerator CaptureViewports(
            Camera camera,
            string directory,
            string state)
        {
            foreach (var viewport in Viewports)
            {
                yield return Capture(
                    camera,
                    directory,
                    state + "-" + viewport.x + "x" + viewport.y + ".png",
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
            AssertStableShellBounds(camera, width, height);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var previous = camera.targetTexture;
            var previousActive = RenderTexture.active;
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
            camera.targetTexture = previous;
            RenderTexture.active = previousActive;
            target.Release();
            Object.Destroy(target);
        }

        private static void AssertStableShellBounds(Camera camera, int width, int height)
        {
            foreach (var name in new[] { "MapViewport", "CombatRoom", "RoomActions" })
            {
                var rect = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                    .FirstOrDefault(candidate =>
                        candidate.gameObject.name == name && candidate.gameObject.activeInHierarchy);
                if (rect == null)
                {
                    continue;
                }

                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    Assert.That(screen.x, Is.InRange(-1f, width + 1f), name + " clipped horizontally.");
                    Assert.That(screen.y, Is.InRange(-1f, height + 1f), name + " clipped vertically.");
                }
            }
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
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

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
                "overworld-map-gate-c");
        }
    }
}
