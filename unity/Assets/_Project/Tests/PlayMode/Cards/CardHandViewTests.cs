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
    public sealed class CardHandViewTests
    {
        private const string LightingResourcePath = "Art/Battle/Cards/lighting";
        private const string LightingStableId = "lighting";

        private GameObject _root;
        private Camera _camera;
        private Canvas _canvas;
        private CardHandView _view;
        private Sprite _sprite;
        private RenderTexture _renderTexture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
            }

            if (_sprite != null)
            {
                UnityEngine.Object.Destroy(_sprite);
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator HoverSelectAndCancel_AreStableAndEmitOnce()
        {
            BuildRig();
            var originalParent = _view.CardVisual.parent;
            var selectedCount = 0;
            var cancelCount = 0;
            _view.CardSelected += stableId =>
            {
                Assert.That(stableId, Is.EqualTo(LightingStableId));
                selectedCount++;
            };
            _view.CardCancelRequested += stableId =>
            {
                Assert.That(stableId, Is.EqualTo(LightingStableId));
                cancelCount++;
            };

            var hover = Pointer(PointerEventData.InputButton.Left);
            _view.OnPointerEnter(hover);
            Assert.That(hover.used, Is.True);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(_view.IsHovered, Is.True);
            Assert.That(_view.CardVisual.anchoredPosition.y, Is.GreaterThan(27f));
            Assert.That(_view.CardVisual.localScale.x, Is.GreaterThan(1.08f));

            var select = Pointer(PointerEventData.InputButton.Left);
            _view.OnPointerClick(select);
            _view.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
            _view.OnPointerExit(Pointer(PointerEventData.InputButton.Left));
            Assert.That(select.used, Is.True);
            Assert.That(selectedCount, Is.EqualTo(1));
            Assert.That(_view.InteractionState, Is.EqualTo(CardHandInteractionState.Selected));
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(_view.CardVisual.anchoredPosition.y, Is.GreaterThan(74f));
            Assert.That(_view.CardVisual.localScale.x, Is.GreaterThan(1.44f));

            var cancel = Pointer(PointerEventData.InputButton.Right);
            _view.OnPointerClick(cancel);
            _view.OnPointerClick(Pointer(PointerEventData.InputButton.Right));
            Assert.That(cancel.used, Is.True);
            Assert.That(cancelCount, Is.EqualTo(1));
            Assert.That(_view.InteractionState, Is.EqualTo(CardHandInteractionState.Idle));
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(_view.CardVisual.parent, Is.SameAs(originalParent));
            Assert.That(_view.CardVisual.anchoredPosition, Is.EqualTo(Vector2.zero).Using(Vector2Comparer(0.2f)));
            Assert.That(_view.CardVisual.localScale, Is.EqualTo(Vector3.one).Using(Vector3Comparer(0.01f)));
            Assert.That(Quaternion.Angle(_view.CardVisual.localRotation, Quaternion.identity), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator Drag_EmitsPhasesConsumesInputAndRestoresPose()
        {
            BuildRig();
            _view.SetInteractionState(CardHandInteractionState.Scheduling);
            var originalParent = _view.CardVisual.parent;
            var phases = new List<CardDragPhase>();
            var positions = new List<Vector2>();
            _view.CardDragChanged += (stableId, position, phase) =>
            {
                Assert.That(stableId, Is.EqualTo(LightingStableId));
                phases.Add(phase);
                positions.Add(position);
            };

            var begin = Pointer(PointerEventData.InputButton.Left, new Vector2(640f, 420f));
            _view.OnBeginDrag(begin);
            Assert.That(begin.used, Is.True);
            Assert.That(_view.IsDragging, Is.True);
            Assert.That(_view.CardVisual.parent, Is.SameAs(_canvas.transform));

            var moved = Pointer(PointerEventData.InputButton.Left, new Vector2(780f, 510f));
            _view.OnDrag(moved);
            var ended = Pointer(PointerEventData.InputButton.Left, new Vector2(820f, 550f));
            _view.OnEndDrag(ended);

            Assert.That(moved.used && ended.used, Is.True);
            Assert.That(phases, Is.EqualTo(new[]
            {
                CardDragPhase.Started,
                CardDragPhase.Moved,
                CardDragPhase.Ended
            }));
            Assert.That(positions, Is.EqualTo(new[]
            {
                new Vector2(640f, 420f),
                new Vector2(780f, 510f),
                new Vector2(820f, 550f)
            }));
            Assert.That(_view.IsDragging, Is.False);
            Assert.That(_view.InteractionState, Is.EqualTo(CardHandInteractionState.Scheduling));
            Assert.That(_view.CardVisual.parent, Is.SameAs(originalParent));

            _view.OnBeginDrag(Pointer(PointerEventData.InputButton.Left, new Vector2(600f, 400f)));
            Assert.That(_view.RequestCancelSelectedCard(), Is.True);
            Assert.That(_view.RequestCancelSelectedCard(), Is.False);
            Assert.That(phases.GetRange(3, 2), Is.EqualTo(new[]
            {
                CardDragPhase.Started,
                CardDragPhase.Cancelled
            }));
            Assert.That(_view.CardVisual.parent, Is.SameAs(originalParent));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(_view.CardVisual.anchoredPosition, Is.EqualTo(Vector2.zero).Using(Vector2Comparer(0.2f)));
            Assert.That(_view.CardVisual.localScale, Is.EqualTo(Vector3.one).Using(Vector3Comparer(0.01f)));
        }

        [UnityTest]
        public IEnumerator RepeatedBuild_DoesNotDuplicateHierarchyOrEventsAndStillBlocksPointers()
        {
            BuildRig();
            var childCount = CountDescendants(_view.transform);
            var selectedCount = 0;
            _view.CardSelected += _ => selectedCount++;

            _view.Build(Model());
            _view.Build(Model());
            Assert.That(CountDescendants(_view.transform), Is.EqualTo(childCount));
            yield return null;
            Canvas.ForceUpdateCanvases();

            var artworkCenter = _view.Artwork.rectTransform.TransformPoint(_view.Artwork.rectTransform.rect.center);
            var raycastPointer = Pointer(
                PointerEventData.InputButton.Left,
                RectTransformUtility.WorldToScreenPoint(_camera, artworkCenter));
            var raycastResults = new List<RaycastResult>();
            _canvas.GetComponent<GraphicRaycaster>().Raycast(raycastPointer, raycastResults);
            Assert.That(raycastResults.Exists(result => result.gameObject == _view.Artwork.gameObject), Is.True);

            var click = Pointer(PointerEventData.InputButton.Left);
            _view.OnPointerDown(click);
            _view.OnPointerClick(click);
            Assert.That(click.used, Is.True);
            Assert.That(selectedCount, Is.EqualTo(1));

            var scroll = Pointer(PointerEventData.InputButton.Middle);
            scroll.scrollDelta = Vector2.up;
            _view.OnScroll(scroll);
            Assert.That(scroll.used, Is.True);

            _view.Build(new CardViewModel(LightingStableId, _sprite, false, false));
            var disabledClick = Pointer(PointerEventData.InputButton.Left);
            _view.OnPointerClick(disabledClick);
            Assert.That(disabledClick.used, Is.True);
            Assert.That(selectedCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RenderedStates_FitThreeViewportsAndHaveDistinctCardPixels()
        {
            BuildRig();
            var viewports = new[]
            {
                new Vector2Int(1920, 1080),
                new Vector2Int(1280, 720),
                new Vector2Int(2560, 1080)
            };

            foreach (var viewport in viewports)
            {
                ConfigureRenderTarget(viewport.x, viewport.y);
                var checksums = new HashSet<ulong>();

                foreach (var state in new[] { "idle", "hover", "selected" })
                {
                    _view.Build(Model());
                    yield return new WaitForSecondsRealtime(0.35f);

                    if (state == "hover")
                    {
                        _view.OnPointerEnter(Pointer(PointerEventData.InputButton.Left));
                        yield return new WaitForSecondsRealtime(0.25f);
                    }
                    else if (state == "selected")
                    {
                        _view.OnPointerClick(Pointer(PointerEventData.InputButton.Left));
                        yield return new WaitForSecondsRealtime(0.3f);
                    }

                    Canvas.ForceUpdateCanvases();
                    AssertCardFits(viewport);
                    var capture = Capture(viewport.x, viewport.y);
                    try
                    {
                        var checksum = CardRegionChecksum(capture);
                        Assert.That(checksum.DistinctColorCount, Is.GreaterThan(32),
                            state + " card region is blank or nearly uniform at " + viewport);
                        Assert.That(checksums.Add(checksum.Value), Is.True,
                            "Rendered card states are not visually distinct at " + viewport);
                        WriteEvidence(capture, viewport, state);
                    }
                    finally
                    {
                        UnityEngine.Object.Destroy(capture);
                    }
                }
            }
        }

        private void BuildRig()
        {
            _root = new GameObject("CardHandTestRig");
            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("CardHandEventSystem");
                eventSystemObject.transform.SetParent(_root.transform, false);
                eventSystemObject.AddComponent<EventSystem>();
            }

            var cameraObject = new GameObject("CardHandCamera");
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.045f, 0.055f, 1f);
            _camera.orthographic = true;

            var canvasObject = new GameObject("CardHandCanvas", typeof(RectTransform));
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

            var texture = Resources.Load<Texture2D>(LightingResourcePath);
            Assert.That(texture, Is.Not.Null, "The original lighting texture was not imported as a Texture2D.");
            _sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _sprite.name = "lighting-card-hand-test-sprite";

            var viewObject = new GameObject("CardHand", typeof(RectTransform));
            viewObject.transform.SetParent(_canvas.transform, false);
            _view = viewObject.AddComponent<CardHandView>();
            _view.Build(Model());
            ConfigureRenderTarget(1920, 1080);
            Canvas.ForceUpdateCanvases();
        }

        private CardViewModel Model()
        {
            return new CardViewModel(LightingStableId, _sprite, false, true);
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
                name = string.Format("CardHand-{0}x{1}", width, height)
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

        private void AssertCardFits(Vector2Int viewport)
        {
            var corners = new Vector3[4];
            _view.CardVisual.GetWorldCorners(corners);
            var maximumY = 0f;
            foreach (var corner in corners)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(_camera, corner);
                Assert.That(screen.x, Is.InRange(0f, (float)viewport.x));
                Assert.That(screen.y, Is.InRange(0f, (float)viewport.y));
                maximumY = Mathf.Max(maximumY, screen.y);
            }

            Assert.That(maximumY, Is.LessThan(viewport.y * 0.45f),
                "The hand card obscures too much of the board viewport.");
            Assert.That(
                _view.Artwork.rectTransform.rect.width / _view.Artwork.rectTransform.rect.height,
                Is.EqualTo(CardHandView.DesignCardSize.x / CardHandView.DesignCardSize.y).Within(0.001f));
            Assert.That(_view.Artwork.preserveAspect, Is.True);
        }

        private static (ulong Value, int DistinctColorCount) CardRegionChecksum(Texture2D image)
        {
            var pixels = image.GetPixels32();
            var distinct = new HashSet<int>();
            ulong checksum = 1469598103934665603UL;
            var minimumY = 0;
            var maximumY = Mathf.Min(image.height, Mathf.CeilToInt(image.height * 0.45f));
            var minimumX = Mathf.FloorToInt(image.width * 0.35f);
            var maximumX = Mathf.CeilToInt(image.width * 0.65f);

            for (var y = minimumY; y < maximumY; y += 3)
            {
                for (var x = minimumX; x < maximumX; x += 3)
                {
                    var color = pixels[(y * image.width) + x];
                    var packed = color.r | (color.g << 8) | (color.b << 16);
                    distinct.Add(packed);
                    checksum ^= (uint)packed;
                    checksum *= 1099511628211UL;
                }
            }

            return (checksum, distinct.Count);
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
                "wave-02b-card-ui-agent");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(
                Path.Combine(directory, string.Format("{0}x{1}-{2}.png", viewport.x, viewport.y, state)),
                capture.EncodeToPNG());
        }

        private PointerEventData Pointer(
            PointerEventData.InputButton button,
            Vector2 position = default)
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

        private static IEqualityComparer<Vector2> Vector2Comparer(float tolerance)
        {
            return new ApproximateVector2Comparer(tolerance);
        }

        private static IEqualityComparer<Vector3> Vector3Comparer(float tolerance)
        {
            return new ApproximateVector3Comparer(tolerance);
        }

        private sealed class ApproximateVector2Comparer : IEqualityComparer<Vector2>
        {
            private readonly float _tolerance;

            public ApproximateVector2Comparer(float tolerance)
            {
                _tolerance = tolerance;
            }

            public bool Equals(Vector2 left, Vector2 right)
            {
                return Vector2.Distance(left, right) <= _tolerance;
            }

            public int GetHashCode(Vector2 value)
            {
                return value.GetHashCode();
            }
        }

        private sealed class ApproximateVector3Comparer : IEqualityComparer<Vector3>
        {
            private readonly float _tolerance;

            public ApproximateVector3Comparer(float tolerance)
            {
                _tolerance = tolerance;
            }

            public bool Equals(Vector3 left, Vector3 right)
            {
                return Vector3.Distance(left, right) <= _tolerance;
            }

            public int GetHashCode(Vector3 value)
            {
                return value.GetHashCode();
            }
        }
    }
}
