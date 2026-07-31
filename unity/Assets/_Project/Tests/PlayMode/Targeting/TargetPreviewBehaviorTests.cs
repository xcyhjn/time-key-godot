using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation.Targeting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Targeting
{
    public sealed class TargetPreviewBehaviorTests
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private readonly List<UnityEngine.Object> _ownedObjects = new List<UnityEngine.Object>();
        private GameObject _root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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

        [Test]
        public void Palette_ProvidesSixDistinctStates()
        {
            var colors = new HashSet<Color>
            {
                TargetPreviewPalette.Normal,
                TargetPreviewPalette.Hover,
                TargetPreviewPalette.Selected,
                TargetPreviewPalette.RangeValid,
                TargetPreviewPalette.TimelineValid,
                TargetPreviewPalette.TimelineInvalid
            };

            Assert.That(colors.Count, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator BoardRange_ProjectsUniqueCoordinatesReportsMissingAndRestoresBlocks()
        {
            _root = new GameObject("BoardRangeBehaviorRig");
            var preview = _root.AddComponent<BoardRangePreview>();
            var sharedMaterial = CreateMaterial(TargetPreviewPalette.Normal);
            var sharedColorBefore = ReadMaterialColor(sharedMaterial);
            var center = CreateCubeTile("Center", sharedMaterial);
            var neighbor = CreateCubeTile("Neighbor", sharedMaterial);
            var untouched = CreateCubeTile("Untouched", sharedMaterial);
            var centerCoord = new HexCoord(0, 0);
            var neighborCoord = new HexCoord(1, 0);
            var untouchedCoord = new HexCoord(0, 1);

            preview.Register(centerCoord, center.transform);
            preview.Register(neighborCoord, neighbor.transform);
            preview.Register(untouchedCoord, untouched.transform);

            var centerRenderer = center.GetComponent<Renderer>();
            var selectedBlock = new MaterialPropertyBlock();
            selectedBlock.SetColor(BaseColorProperty, TargetPreviewPalette.Selected);
            centerRenderer.SetPropertyBlock(selectedBlock);

            var missingEvents = new List<HexCoord>();
            preview.MissingCoordinate += missingEvents.Add;
            var lightingOffsets = new[]
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0)
            };

            preview.Show(centerCoord, lightingOffsets);
            Assert.That(preview.ActiveCoordinates, Is.EqualTo(new[] { centerCoord, neighborCoord }));
            Assert.That(preview.MissingCoordinates, Is.EqualTo(new[] { new HexCoord(2, 0) }));
            Assert.That(missingEvents, Is.EqualTo(preview.MissingCoordinates));
            AssertRendererColor(centerRenderer, TargetPreviewPalette.RangeValid);
            AssertRendererColor(neighbor.GetComponent<Renderer>(), TargetPreviewPalette.RangeValid);
            AssertRendererHasNoColor(untouched.GetComponent<Renderer>());
            AssertColor(ReadMaterialColor(sharedMaterial), sharedColorBefore);

            preview.Show(centerCoord, lightingOffsets);
            Assert.That(preview.ActiveCoordinates.Count, Is.EqualTo(2));
            preview.Clear();
            preview.Clear();

            AssertRendererColor(centerRenderer, TargetPreviewPalette.Selected);
            AssertRendererHasNoColor(neighbor.GetComponent<Renderer>());
            Assert.That(preview.ActiveCoordinates, Is.Empty);
            Assert.That(preview.MissingCoordinates, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimelinePreview_UsesDomainValidityAndRestoresPerCellColors()
        {
            _root = new GameObject("TimelineBehaviorRig");
            var preview = _root.AddComponent<TimelinePlacementPreview>();
            var sharedMaterial = CreateMaterial(Color.white);
            var sharedColorBefore = ReadMaterialColor(sharedMaterial);
            var previewCell = CreateImageCell("PreviewCell", sharedMaterial, new Color(0.18f, 0.20f, 0.22f, 1f));
            var untouchedCell = CreateImageCell("UntouchedCell", sharedMaterial, new Color(0.30f, 0.31f, 0.32f, 1f));
            var previewCoord = new TimelineCell(4, 1);
            var untouchedCoord = new TimelineCell(5, 1);
            preview.Register(previewCoord, previewCell);
            preview.Register(untouchedCoord, untouchedCell);

            var card = CreateLightingCard();
            var grid = new TimelineGrid();
            var session = new CardPlaySession(card);
            session.SelectTarget("target-01", new HexCoord(0, 0));
            var validTransition = session.PreviewTimeline(grid, previewCoord);

            preview.Show(previewCoord, card.Shape, validTransition.IsPlacementValid);
            Assert.That(validTransition.IsPlacementValid, Is.True);
            Assert.That(preview.IsValid, Is.True);
            Assert.That(preview.ActiveCoordinates, Is.EqualTo(new[] { previewCoord }));
            AssertColor(previewCell.color, TargetPreviewPalette.TimelineValid);
            AssertColor(untouchedCell.color, new Color(0.30f, 0.31f, 0.32f, 1f));
            Assert.That(previewCell.material, Is.SameAs(sharedMaterial));
            AssertColor(ReadMaterialColor(sharedMaterial), sharedColorBefore);

            preview.Clear();
            AssertColor(previewCell.color, new Color(0.18f, 0.20f, 0.22f, 1f));
            var blockingAction = new TimelineAction(
                TimelineActorKind.Enemy,
                "blocker",
                "target-01",
                previewCoord,
                new[] { new TimelineCell(0, 0) },
                0);
            Assert.That(grid.TryPlace(blockingAction), Is.True);
            var invalidTransition = session.PreviewTimeline(grid, previewCoord);

            preview.Show(previewCoord, card.Shape, invalidTransition.IsPlacementValid);
            Assert.That(invalidTransition.IsPlacementValid, Is.False);
            Assert.That(preview.IsValid, Is.False);
            AssertColor(previewCell.color, TargetPreviewPalette.TimelineInvalid);

            preview.Show(new TimelineCell(12, 0), card.Shape, false);
            Assert.That(preview.ActiveCoordinates, Is.Empty);
            Assert.That(preview.MissingCoordinates, Is.EqualTo(new[] { new TimelineCell(12, 0) }));
            preview.Clear();
            preview.Clear();
            AssertColor(previewCell.color, new Color(0.18f, 0.20f, 0.22f, 1f));
            yield return null;
        }

        private GameObject CreateCubeTile(string name, Material material)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = name;
            tile.transform.SetParent(_root.transform, false);
            tile.GetComponent<Renderer>().sharedMaterial = material;
            return tile;
        }

        private Image CreateImageCell(string name, Material material, Color color)
        {
            var cell = new GameObject(name, typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(_root.transform, false);
            var image = cell.GetComponent<Image>();
            image.material = material;
            image.color = color;
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

        private static CardDefinition CreateLightingCard()
        {
            return new CardDefinition(
                "lighting",
                1,
                new[] { new CardEffect(CardEffectKind.Damage, 100) },
                new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) },
                new[] { new TimelineCell(0, 0) });
        }

        private static Color ReadMaterialColor(Material material)
        {
            return material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.GetColor("_Color");
        }

        private static void AssertRendererColor(Renderer renderer, Color expected)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            AssertColor(block.GetColor(BaseColorProperty), expected);
        }

        private static void AssertRendererHasNoColor(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.True);
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(Vector4.Distance(actual, expected), Is.LessThan(0.001f));
        }
    }
}
