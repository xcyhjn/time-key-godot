using System.Collections;
using NUnit.Framework;
using TimeKey.Presentation.Terrain;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.Terrain
{
    public sealed class HexTileColumnTests
    {
        private GameObject _root;
        private GameObject _grassPrefab;
        private GameObject _dirtPrefab;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyLogicalLayerCount_UsesIndependentBlocksAndBounds()
        {
            var column = CreateColumn();
            column.ApplyLogicalLayerCount(1);
            column.ApplyLogicalLayerCount(3);
            yield return null;

            Assert.That(column.LayerCount, Is.EqualTo(3));
            Assert.That(column.Blocks.Count, Is.EqualTo(3));
            Assert.That(column.Renderers.Count, Is.EqualTo(3));
            Assert.That(column.Colliders.Count, Is.EqualTo(3));
            Assert.That(column.Blocks[0].transform.localPosition.y, Is.Zero.Within(0.001f));
            Assert.That(column.Blocks[1].transform.localPosition.y, Is.EqualTo(0.32f).Within(0.001f));
            Assert.That(column.Blocks[2].transform.localPosition.y, Is.EqualTo(0.64f).Within(0.001f));
            Assert.That(column.TopBounds.max.y, Is.GreaterThan(0.64f));
            Assert.That(column.OccupantAnchor.position.y, Is.EqualTo(column.TopBounds.max.y).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ApplyLogicalLayerCount_IsIdempotentAndShrinksWithoutGhosts()
        {
            var column = CreateColumn();
            column.ApplyLogicalLayerCount(3);
            var topBlock = column.Blocks[2];
            column.ApplyLogicalLayerCount(3);
            Assert.That(column.Blocks[2], Is.SameAs(topBlock));

            column.ApplyLogicalLayerCount(1);
            yield return null;

            Assert.That(column.LayerCount, Is.EqualTo(1));
            Assert.That(column.Blocks.Count, Is.EqualTo(1));
            Assert.That(column.transform.Find("Block-1"), Is.Null);
            Assert.That(column.transform.Find("Block-2"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Clear_RemovesAllBlocksAndResetsAnchor()
        {
            var column = CreateColumn();
            column.ApplyLogicalLayerCount(3);
            column.Clear();
            yield return null;

            Assert.That(column.LayerCount, Is.Zero);
            Assert.That(column.Blocks.Count, Is.Zero);
            Assert.That(column.Renderers.Count, Is.Zero);
            Assert.That(column.Colliders.Count, Is.Zero);
            Assert.That(column.OccupantAnchor.position, Is.EqualTo(column.transform.position));
        }

        private HexTileColumn CreateColumn()
        {
            _root = new GameObject("HexTileColumnTestRoot");
            _grassPrefab = CreatePrefab("Grass");
            _dirtPrefab = CreatePrefab("Dirt");
            var columnObject = new GameObject("HexTileColumn");
            columnObject.transform.SetParent(_root.transform, false);
            var column = columnObject.AddComponent<HexTileColumn>();
            column.Initialize(_grassPrefab, _dirtPrefab);
            return column;
        }

        private static GameObject CreatePrefab(string name)
        {
            var prefab = new GameObject(name);
            prefab.AddComponent<MeshFilter>().sharedMesh = CreateMesh();
            prefab.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial();
            prefab.SetActive(false);
            return prefab;
        }

        private static Mesh CreateMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0.2f, 0.5f), new Vector3(-0.5f, 0.2f, 0.5f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Unlit/Color");
            return new Material(shader);
        }
    }
}
