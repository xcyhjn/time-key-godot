using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.OverworldMovement;
using TimeKey.Presentation.OutOfBattleShell;
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
        private static readonly Vector2Int[] Viewports =
        {
            new Vector2Int(1920, 1080),
            new Vector2Int(1280, 720),
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
        public IEnumerator Movement_RejectsDragAndSameNode_AndCapturesAnimationFrames()
        {
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);
            _eventSystem = new GameObject(
                "OverworldMovementEventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            var eventSystem = _eventSystem.GetComponent<EventSystem>();

            yield return LoadSingle("OutOfBattleShell");
            var entry = Object.FindFirstObjectByType<SceneContentEntry>(FindObjectsInactive.Include);
            Assert.That(entry, Is.Not.Null);
            var start = new RunStartPayload(
                RunStartKind.SeedGame,
                "wave-03p-visual-run",
                731,
                "731",
                1,
                2,
                3,
                64,
                "silver-character",
                new[] { "card.wind", "card.recover" });
            entry.Bind(start);
            entry.SetCameraEnabled(true);
            yield return null;
            var camera = Object.FindFirstObjectByType<Camera>();
            var presenter = Object.FindFirstObjectByType<OutOfBattleShellPresenter>();
            var movement = Object.FindFirstObjectByType<OverworldMovementPresenter>();
            var input = Object.FindFirstObjectByType<OverworldMapInputController>();
            Assert.That(camera, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(movement, Is.Not.Null);
            Assert.That(input, Is.Not.Null);

            PrepareCanvas(camera);
            presenter.Apply(new OutOfBattleShellState(start));
            var unlockDeadline = Time.realtimeSinceStartup + 5f;
            while (SceneInputLockState.IsLocked && Time.realtimeSinceStartup < unlockDeadline)
            {
                yield return null;
            }

            Assert.That(SceneInputLockState.IsLocked, Is.False, "Scene reveal did not release input.");
            yield return CaptureViewports(camera, evidence, "start");

            var startButton = FindNodeButton(movement, "start");
            Assert.That(movement.TryMove(
                startButton.GetComponent<OverworldNodeView>().Id), Is.False);
            Assert.That(movement.IsMoving, Is.False);
            Assert.That(FindStatusText(movement).text, Does.Contain("无需移动"));
            yield return Capture(camera, evidence, "illegal-same-node-1280x720.png", 1280, 720);

            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = Vector2.zero
            };
            input.OnBeginDrag(pointer);
            pointer.position = new Vector2(32f, 0f);
            input.OnDrag(pointer);
            input.OnEndDrag(pointer);
            var room01Button = FindNodeButton(movement, "room-01");
            room01Button.onClick.Invoke();
            Assert.That(movement.IsMoving, Is.False);
            yield return Capture(camera, evidence, "drag-suppressed-1280x720.png", 1280, 720);

            input.BeginPointerGesture();
            Assert.That(movement.TryMove(
                room01Button.GetComponent<OverworldNodeView>().Id), Is.True);
            yield return new WaitForSecondsRealtime(0.14f);
            yield return Capture(camera, evidence, "moving-1920x1080.png", 1920, 1080);
            while (movement.IsMoving)
            {
                yield return null;
            }

            Assert.That(movement.Snapshot.CurrentNodeId.Value, Is.EqualTo("room-01"));
            Assert.That(FindStatusText(movement).text, Does.Contain("room-01"));
            yield return CaptureViewports(camera, evidence, "arrived-room-01");

            movement.enabled = false;
            yield return null;
            movement.enabled = true;
            yield return null;
            Assert.That(movement.Snapshot.CurrentNodeId.Value, Is.EqualTo("room-01"));

            movement.SetTransitionLocked(true);
            var room02Button = FindNodeButton(movement, "room-02");
            room02Button.onClick.Invoke();
            Assert.That(movement.IsMoving, Is.False);
            movement.SetTransitionLocked(false);
            Screen.SetResolution(1600, 900, false);
            yield return null;
            yield return Capture(camera, evidence, "dynamic-resize-1600x900.png", 1600, 900);
        }

        private static Button FindNodeButton(OverworldMovementPresenter presenter, string id)
        {
            foreach (var node in presenter.GetComponentsInChildren<OverworldNodeView>(true))
            {
                if (node.Id.Value == id)
                {
                    return node.GetComponent<Button>();
                }
            }

            Assert.Fail("Missing node button: " + id);
            return null;
        }

        private static Text FindStatusText(OverworldMovementPresenter presenter)
        {
            foreach (var text in presenter.GetComponentsInChildren<Text>(true))
            {
                if (text.name == "MovementStatus")
                {
                    return text;
                }
            }

            Assert.Fail("Missing MovementStatus text.");
            return null;
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
                "overworld-movement-theme-gate-d");
        }
    }
}
