using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation.Targeting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Targeting
{
    public sealed class TargetPreviewVisualTests
    {
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const float BlockHeight = 0.32f;

        private readonly List<UnityEngine.Object> _ownedObjects = new List<UnityEngine.Object>();
        private GameObject _root;
        private Camera _camera;
        private RenderTexture _renderTexture;
        private Mesh _hexMesh;
        private BoardRangePreview _boardPreview;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            for (var index = 0; index < _ownedObjects.Count; index++)
            {
                if (_ownedObjects[index] != null)
                {
                    UnityEngine.Object.Destroy(_ownedObjects[index]);
                }
            }

            _ownedObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CardinalYaw_KeepsRangeCoordinatesAndRendersStackedHighlightsAndInvalidTimeline()
        {
            BuildBoardRig();
            var center = new HexCoord(0, 0);
            var lightingRange = new[]
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };
            _boardPreview.Show(center, lightingRange);
            var expectedCoordinates = new[]
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };
            var viewChecksums = new HashSet<ulong>();

            foreach (var yaw in new[] { 0, 90, 180, 270 })
            {
                SetCameraView(yaw);
                Assert.That(_boardPreview.ActiveCoordinates, Is.EqualTo(expectedCoordinates));
                var capture = Capture();
                try
                {
                    Assert.That(CountPixelsNear(capture, TargetPreviewPalette.RangeValid), Is.GreaterThan(2500),
                        "Range highlight is not legible at yaw " + yaw + ".");
                    Assert.That(viewChecksums.Add(Checksum(capture)), Is.True,
                        "Cardinal evidence frames should depict distinct viewpoints.");
                    WriteEvidence(capture, "board-range-yaw-" + yaw + ".png");
                }
                finally
                {
                    UnityEngine.Object.Destroy(capture);
                }

                yield return null;
            }

            var timeline = BuildTimeline();
            timeline.Show(
                new TimelineCell(5, 1),
                new[] { new TimelineCell(0, 0) },
                false);
            Canvas.ForceUpdateCanvases();
            SetCameraView(32f);
            var invalidCapture = Capture();
            try
            {
                Assert.That(CountPixelsNear(invalidCapture, TargetPreviewPalette.TimelineInvalid), Is.GreaterThan(500),
                    "The invalid timeline state is not legible.");
                Assert.That(CountPixelsNear(invalidCapture, TargetPreviewPalette.RangeValid), Is.GreaterThan(2500),
                    "The high stacked board range should remain visible with the timeline preview.");
                WriteEvidence(invalidCapture, "timeline-invalid.png");
            }
            finally
            {
                UnityEngine.Object.Destroy(invalidCapture);
            }
        }

        private void BuildBoardRig()
        {
            _root = new GameObject("TargetPreviewVisualRig");
            _boardPreview = _root.AddComponent<BoardRangePreview>();
            _hexMesh = CreateHexMesh(0.94f, BlockHeight);
            _ownedObjects.Add(_hexMesh);

            var cameraObject = new GameObject("TargetPreviewCamera");
            cameraObject.transform.SetParent(_root.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.035f, 0.04f, 1f);
            _camera.fieldOfView = 38f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 100f;
            _renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "TargetPreviewEvidence"
            };
            _renderTexture.Create();
            _camera.targetTexture = _renderTexture;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "EvidenceGround";
            ground.transform.SetParent(_root.transform, false);
            ground.transform.position = new Vector3(0f, -0.04f, 0f);
            ground.transform.localScale = new Vector3(2.2f, 1f, 2.2f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.035f, 0.055f, 0.05f, 1f));

            var tileMaterial = CreateMaterial(TargetPreviewPalette.Normal);
            for (var q = -2; q <= 2; q++)
            {
                var minimumR = Mathf.Max(-2, -q - 2);
                var maximumR = Mathf.Min(2, -q + 2);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    var coordinate = new HexCoord(q, r);
                    var elevation = coordinate == new HexCoord(1, 0) || coordinate == new HexCoord(-1, 1)
                        ? 1
                        : 0;
                    CreateStackedTile(coordinate, elevation, tileMaterial);
                }
            }
        }

        private void CreateStackedTile(HexCoord coordinate, int elevation, Material material)
        {
            var tile = new GameObject(string.Format("Hex-{0}-{1}", coordinate.Q, coordinate.R));
            tile.transform.SetParent(_root.transform, false);
            tile.transform.position = HexToWorld(coordinate);
            for (var layer = 0; layer <= elevation; layer++)
            {
                var block = new GameObject("Block-" + layer, typeof(MeshFilter), typeof(MeshRenderer));
                block.transform.SetParent(tile.transform, false);
                block.transform.localPosition = new Vector3(0f, layer * BlockHeight, 0f);
                block.GetComponent<MeshFilter>().sharedMesh = _hexMesh;
                block.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            _boardPreview.Register(coordinate, tile.transform);
        }

        private TimelinePlacementPreview BuildTimeline()
        {
            var canvasObject = new GameObject("TimelineCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(_root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CaptureWidth, CaptureHeight);

            var panel = new GameObject("TimelinePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(720f, 142f);
            panelRect.anchoredPosition = new Vector2(0f, -20f);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.065f, 0.98f);
            var preview = panel.AddComponent<TimelinePlacementPreview>();

            const float cellWidth = 52f;
            const float cellHeight = 30f;
            const float spacing = 4f;
            var startX = -((12f * cellWidth) + (11f * spacing)) * 0.5f + (cellWidth * 0.5f);
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 12; x++)
                {
                    var cellObject = new GameObject(
                        string.Format("Timeline-{0}-{1}", x, y),
                        typeof(RectTransform),
                        typeof(Image));
                    cellObject.transform.SetParent(panel.transform, false);
                    var rect = cellObject.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(cellWidth, cellHeight);
                    rect.anchoredPosition = new Vector2(
                        startX + (x * (cellWidth + spacing)),
                        -28f - (y * (cellHeight + spacing)));
                    var image = cellObject.GetComponent<Image>();
                    image.color = new Color(0.16f, 0.19f, 0.20f, 1f);
                    preview.Register(new TimelineCell(x, y), image);
                }
            }

            return preview;
        }

        private void SetCameraView(float yaw)
        {
            var pivot = new Vector3(0f, 0.45f, 0f);
            var orbit = Quaternion.Euler(48f, yaw, 0f);
            _camera.transform.position = pivot + (orbit * Vector3.back * 14.5f);
            _camera.transform.rotation = Quaternion.LookRotation(pivot - _camera.transform.position, Vector3.up);
        }

        private Texture2D Capture()
        {
            _camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0, false);
            image.Apply(false, false);
            RenderTexture.active = previous;
            return image;
        }

        private Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            _ownedObjects.Add(material);
            return material;
        }

        private static Mesh CreateHexMesh(float radius, float height)
        {
            var vertices = new Vector3[12];
            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index);
                vertices[index] = new Vector3(radius * Mathf.Cos(angle), height, radius * Mathf.Sin(angle));
                vertices[index + 6] = new Vector3(vertices[index].x, 0f, vertices[index].z);
            }

            var triangles = new int[60];
            var cursor = 0;
            for (var index = 1; index < 5; index++)
            {
                triangles[cursor++] = 0;
                triangles[cursor++] = index;
                triangles[cursor++] = index + 1;
                triangles[cursor++] = 6;
                triangles[cursor++] = index + 7;
                triangles[cursor++] = index + 6;
            }

            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                triangles[cursor++] = index;
                triangles[cursor++] = next;
                triangles[cursor++] = index + 6;
                triangles[cursor++] = next;
                triangles[cursor++] = next + 6;
                triangles[cursor++] = index + 6;
            }

            var mesh = new Mesh { name = "Target Preview Hex" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 HexToWorld(HexCoord coordinate)
        {
            return new Vector3(
                1.5f * coordinate.Q,
                0f,
                Mathf.Sqrt(3f) * (coordinate.R + (coordinate.Q * 0.5f)));
        }

        private static int CountPixelsNear(Texture2D image, Color expected)
        {
            var target = (Color32)expected;
            var count = 0;
            foreach (var pixel in image.GetPixels32())
            {
                if (Mathf.Abs(pixel.r - target.r) <= 12 &&
                    Mathf.Abs(pixel.g - target.g) <= 12 &&
                    Mathf.Abs(pixel.b - target.b) <= 12)
                {
                    count++;
                }
            }

            return count;
        }

        private static ulong Checksum(Texture2D image)
        {
            var pixels = image.GetPixels32();
            ulong checksum = 1469598103934665603UL;
            for (var index = 0; index < pixels.Length; index += 7)
            {
                checksum ^= pixels[index].r;
                checksum *= 1099511628211UL;
                checksum ^= pixels[index].g;
                checksum *= 1099511628211UL;
                checksum ^= pixels[index].b;
                checksum *= 1099511628211UL;
            }

            return checksum;
        }

        private static void WriteEvidence(Texture2D image, string fileName)
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
                "wave-02b-targeting-agent");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), image.EncodeToPNG());
        }
    }
}
