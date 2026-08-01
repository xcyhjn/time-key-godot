using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Presentation.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Cards
{
    public sealed class CardHandHostTests
    {
        private const string LightingId = "lighting";
        private const string EarthquakeId = "earthquake";
        private const string LightingResourcePath = "Art/Battle/Cards/lighting";
        private const string EarthquakeResourcePath = "Art/Battle/Cards/earthquake";

        private readonly List<Sprite> _createdSprites = new List<Sprite>();
        private GameObject _root;
        private Camera _camera;
        private Canvas _canvas;
        private CardHandHost _host;
        private Sprite _lightingSprite;
        private Sprite _earthquakeSprite;
        private RenderTexture _renderTexture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
            }

            foreach (var sprite in _createdSprites)
            {
                UnityEngine.Object.Destroy(sprite);
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Build_MapsOrderedIdsAndIsIdempotent()
        {
            BuildRig();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var lighting = _host.GetCard(LightingId);
            var earthquake = _host.GetCard(EarthquakeId);
            var descendantCount = CountDescendants(_host.transform);
            var selectedCount = 0;
            _host.CardSelected += _ => selectedCount++;

            Assert.That(_host.CardCount, Is.EqualTo(2));
            Assert.That(_host.Cards[0].StableId, Is.EqualTo(LightingId));
            Assert.That(_host.Cards[1].StableId, Is.EqualTo(EarthquakeId));
            Assert.That(lighting, Is.Not.Null);
            Assert.That(earthquake, Is.Not.Null);
            Assert.That(
                Mathf.Abs(((RectTransform)lighting.transform).anchoredPosition.x -
                    ((RectTransform)earthquake.transform).anchoredPosition.x),
                Is.InRange(92f, 126f));

            _host.Build(Models());
            _host.Build(Models());
            yield return null;

            Assert.That(_host.CardCount, Is.EqualTo(2));
            Assert.That(_host.GetCard(LightingId), Is.SameAs(lighting));
            Assert.That(_host.GetCard(EarthquakeId), Is.SameAs(earthquake));
            Assert.That(CountDescendants(_host.transform), Is.EqualTo(descendantCount));
            lighting.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
            Assert.That(selectedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SelectingSecondCard_CancelsFirstAndRestoresBothPoses()
        {
            BuildRig();
            var selected = new List<string>();
            var cancelled = new List<string>();
            _host.CardSelected += selected.Add;
            _host.CardCancelRequested += cancelled.Add;
            var lighting = _host.GetCard(LightingId);
            var earthquake = _host.GetCard(EarthquakeId);

            lighting.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
            earthquake.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
            _host.ApplyVisualStateImmediate();

            Assert.That(selected, Is.EqualTo(new[] { LightingId, EarthquakeId }));
            Assert.That(cancelled, Is.EqualTo(new[] { LightingId }));
            Assert.That(_host.SelectedStableId, Is.EqualTo(EarthquakeId));
            Assert.That(lighting.InteractionState, Is.EqualTo(CardHandInteractionState.Idle));
            Assert.That(earthquake.InteractionState, Is.EqualTo(CardHandInteractionState.Selected));
            Assert.That(lighting.CardVisual.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(earthquake.CardVisual.anchoredPosition.y, Is.EqualTo(48f).Within(0.01f));
            Assert.That(earthquake.transform.GetSiblingIndex(), Is.EqualTo(earthquake.transform.parent.childCount - 1));

            var cancel = Pointer(PointerEventData.InputButton.Right);
            earthquake.OnPointerClick(cancel);
            _host.ApplyVisualStateImmediate();

            Assert.That(cancel.used, Is.True);
            Assert.That(cancelled, Is.EqualTo(new[] { LightingId, EarthquakeId }));
            Assert.That(_host.SelectedStableId, Is.Null);
            Assert.That(earthquake.InteractionState, Is.EqualTo(CardHandInteractionState.Idle));
            Assert.That(earthquake.CardVisual.anchoredPosition, Is.EqualTo(Vector2.zero));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DragEvents_BelongToOriginatingCardAndRestoreParent()
        {
            BuildRig();
            var lighting = _host.GetCard(LightingId);
            var stableIds = new List<string>();
            var phases = new List<CardDragPhase>();
            _host.CardDragChanged += (stableId, _, phase) =>
            {
                stableIds.Add(stableId);
                phases.Add(phase);
            };

            lighting.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
            var originalParent = lighting.CardVisual.parent;
            lighting.OnBeginDrag(Pointer(PointerEventData.InputButton.Left, new Vector2(620f, 420f)));
            lighting.OnDrag(Pointer(PointerEventData.InputButton.Left, new Vector2(700f, 470f)));
            lighting.OnEndDrag(Pointer(PointerEventData.InputButton.Left, new Vector2(760f, 520f)));

            Assert.That(stableIds, Is.EqualTo(new[] { LightingId, LightingId, LightingId }));
            Assert.That(phases, Is.EqualTo(new[]
            {
                CardDragPhase.Started,
                CardDragPhase.Moved,
                CardDragPhase.Ended
            }));
            Assert.That(lighting.IsDragging, Is.False);
            Assert.That(lighting.CardVisual.parent, Is.SameAs(originalParent));
            Assert.That(_host.SelectedStableId, Is.EqualTo(LightingId));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledHost_ConsumesPointersWithoutSelection()
        {
            BuildRig();
            var selectedCount = 0;
            _host.CardSelected += _ => selectedCount++;
            _host.SetInteractionState(CardHandInteractionState.Disabled);
            var earthquake = _host.GetCard(EarthquakeId);

            var down = Pointer(PointerEventData.InputButton.Left);
            earthquake.OnPointerDown(down);
            var click = Pointer(PointerEventData.InputButton.Left);
            earthquake.OnPointerClick(click);
            var scroll = Pointer(PointerEventData.InputButton.Middle);
            scroll.scrollDelta = Vector2.up;
            earthquake.OnScroll(scroll);

            Assert.That(down.used && click.used && scroll.used, Is.True);
            Assert.That(selectedCount, Is.Zero);
            Assert.That(_host.SelectedStableId, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RenderedTwoCardStates_FitThreeViewportsAndWriteEvidence()
        {
            BuildRig();
            var viewports = new[]
            {
                new Vector2Int(1920, 1080),
                new Vector2Int(1280, 720),
                new Vector2Int(2560, 1080)
            };
            var states = new[] { "idle", "lighting-selected", "earthquake-selected" };

            foreach (var viewport in viewports)
            {
                ConfigureRenderTarget(viewport.x, viewport.y);
                foreach (var state in states)
                {
                    var selectedId = state == "idle"
                        ? null
                        : state == "lighting-selected" ? LightingId : EarthquakeId;
                    _host.Build(Models(selectedId));
                    _host.ApplyVisualStateImmediate();
                    Canvas.ForceUpdateCanvases();

                    AssertCardsFit(viewport);
                    var capture = Capture(viewport.x, viewport.y);
                    try
                    {
                        AssertArtworkIsDistinct(capture, _host.GetCard(LightingId), viewport);
                        AssertArtworkIsDistinct(capture, _host.GetCard(EarthquakeId), viewport);
                        WriteEvidence(capture, viewport, state);
                    }
                    finally
                    {
                        UnityEngine.Object.Destroy(capture);
                    }

                    yield return null;
                }
            }
        }

        private void BuildRig()
        {
            _root = new GameObject("CardHandHostTestRig");
            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("CardHandHostEventSystem");
                eventSystemObject.transform.SetParent(_root.transform, false);
                eventSystemObject.AddComponent<EventSystem>();
            }

            var cameraObject = new GameObject("CardHandHostCamera");
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.045f, 0.055f, 1f);
            _camera.orthographic = true;

            var canvasObject = new GameObject("CardHandHostCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(_root.transform, false);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _camera;
            _canvas.planeDistance = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropObject.transform.SetParent(_canvas.transform, false);
            var backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdrop = backdropObject.GetComponent<Image>();
            backdrop.color = new Color(0.035f, 0.075f, 0.07f, 1f);
            backdrop.raycastTarget = false;

            _lightingSprite = LoadSprite(LightingResourcePath, "lighting-host-test-sprite");
            _earthquakeSprite = LoadSprite(EarthquakeResourcePath, "earthquake-host-test-sprite");
            var hostObject = new GameObject("CardHandHost", typeof(RectTransform));
            hostObject.transform.SetParent(_canvas.transform, false);
            _host = hostObject.AddComponent<CardHandHost>();
            _host.Build(Models());
            ConfigureRenderTarget(1920, 1080);
            Canvas.ForceUpdateCanvases();
        }

        private CardViewModel[] Models(string selectedStableId = null, bool interactable = true)
        {
            return new[]
            {
                new CardViewModel(LightingId, _lightingSprite, selectedStableId == LightingId, interactable),
                new CardViewModel(EarthquakeId, _earthquakeSprite, selectedStableId == EarthquakeId, interactable)
            };
        }

        private Sprite LoadSprite(string resourcePath, string name)
        {
            var importedSprite = Resources.Load<Sprite>(resourcePath);
            if (importedSprite != null)
            {
                return importedSprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            Assert.That(texture, Is.Not.Null, "Card texture could not be loaded: " + resourcePath);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            _createdSprites.Add(sprite);
            return sprite;
        }

        private void ConfigureRenderTarget(int width, int height)
        {
            if (_renderTexture != null)
            {
                _camera.targetTexture = null;
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
            }

            _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = string.Format("CardHandHost-{0}x{1}", width, height)
            };
            _renderTexture.Create();
            _camera.targetTexture = _renderTexture;
            Canvas.ForceUpdateCanvases();
        }

        private Texture2D Capture(int width, int height)
        {
            _camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            image.Apply(false, false);
            RenderTexture.active = previous;
            return image;
        }

        private void AssertCardsFit(Vector2Int viewport)
        {
            foreach (var card in _host.Cards)
            {
                var corners = new Vector3[4];
                card.CardVisual.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var screen = RectTransformUtility.WorldToScreenPoint(_camera, corner);
                    Assert.That(screen.x, Is.InRange(0f, (float)viewport.x));
                    Assert.That(screen.y, Is.InRange(0f, (float)viewport.y));
                    Assert.That(screen.y, Is.LessThan(viewport.y * 0.5f));
                }

                Assert.That(
                    card.Artwork.rectTransform.rect.width / card.Artwork.rectTransform.rect.height,
                    Is.EqualTo(CardHandView.DesignCardSize.x / CardHandView.DesignCardSize.y).Within(0.001f));
                Assert.That(card.Artwork.preserveAspect, Is.True);
            }
        }

        private void AssertArtworkIsDistinct(Texture2D capture, CardHandView card, Vector2Int viewport)
        {
            var corners = new Vector3[4];
            card.Artwork.rectTransform.GetWorldCorners(corners);
            var lowerLeft = RectTransformUtility.WorldToScreenPoint(_camera, corners[0]);
            var upperRight = RectTransformUtility.WorldToScreenPoint(_camera, corners[2]);
            var minimumX = Mathf.Clamp(Mathf.FloorToInt(lowerLeft.x), 0, viewport.x - 1);
            var maximumX = Mathf.Clamp(Mathf.CeilToInt(upperRight.x), minimumX + 1, viewport.x);
            var minimumY = Mathf.Clamp(Mathf.FloorToInt(lowerLeft.y), 0, viewport.y - 1);
            var maximumY = Mathf.Clamp(Mathf.CeilToInt(upperRight.y), minimumY + 1, viewport.y);
            var pixels = capture.GetPixels32();
            var distinct = new HashSet<int>();

            for (var y = minimumY; y < maximumY; y += 3)
            {
                for (var x = minimumX; x < maximumX; x += 3)
                {
                    var color = pixels[(y * viewport.x) + x];
                    distinct.Add(color.r | (color.g << 8) | (color.b << 16));
                }
            }

            Assert.That(distinct.Count, Is.GreaterThan(32), card.StableId + " artwork is blank or uniform.");
        }

        private static void WriteEvidence(Texture2D capture, Vector2Int viewport, string state)
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                return;
            }

            var directory = Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "wave-02b2a-two-card-hand-agent");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(
                Path.Combine(directory, string.Format(
                    "two-card-hand-{0}x{1}-{2}.png",
                    viewport.x,
                    viewport.y,
                    state)),
                capture.EncodeToPNG());
        }

        private PointerEventData Pointer(PointerEventData.InputButton button, Vector2 position = default)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = button,
                position = position
            };
        }

        private static int CountDescendants(Transform root)
        {
            var count = 0;
            foreach (Transform child in root)
            {
                count += 1 + CountDescendants(child);
            }

            return count;
        }
    }
}
