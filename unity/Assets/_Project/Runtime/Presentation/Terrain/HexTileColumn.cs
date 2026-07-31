using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeKey.Presentation.Terrain
{
    public sealed class HexTileColumn : MonoBehaviour
    {
        public const float DefaultLayerSpacing = 0.32f;

        private readonly List<GameObject> _blocks = new List<GameObject>();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Collider> _colliders = new List<Collider>();
        private GameObject _grassPrefab;
        private GameObject _dirtPrefab;
        private float _layerSpacing = DefaultLayerSpacing;
        private Transform _occupantAnchor;
        private bool _usesDirt;

        public event Action<HexTileColumn> Changed;

        public int LayerCount { get; private set; }

        public IReadOnlyList<GameObject> Blocks => _blocks;

        public IReadOnlyList<Renderer> Renderers => _renderers;

        public IReadOnlyList<Collider> Colliders => _colliders;

        public Bounds TopBounds { get; private set; }

        public Transform OccupantAnchor => _occupantAnchor;

        public float LayerSpacing => _layerSpacing;

        public void Initialize(GameObject grassPrefab, GameObject dirtPrefab, float layerSpacing = DefaultLayerSpacing)
        {
            if (grassPrefab == null || dirtPrefab == null)
            {
                throw new ArgumentNullException(nameof(grassPrefab));
            }

            if (layerSpacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(layerSpacing));
            }

            _grassPrefab = grassPrefab;
            _dirtPrefab = dirtPrefab;
            _layerSpacing = layerSpacing;
            EnsureOccupantAnchor();
            RefreshBounds();
        }

        public void ApplyLogicalLayerCount(int logicalLayerCount)
        {
            if (logicalLayerCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalLayerCount));
            }

            if (logicalLayerCount == LayerCount)
            {
                RefreshBounds();
                return;
            }

            if (_grassPrefab == null || _dirtPrefab == null)
            {
                throw new InvalidOperationException("HexTileColumn must be initialized before applying layers.");
            }

            var useDirt = logicalLayerCount > 1;
            if (_blocks.Count > 0 && useDirt != _usesDirt)
            {
                while (_blocks.Count > 0)
                {
                    var last = _blocks[_blocks.Count - 1];
                    _blocks.RemoveAt(_blocks.Count - 1);
                    DestroyOwned(last);
                }
            }

            while (_blocks.Count > logicalLayerCount)
            {
                var last = _blocks[_blocks.Count - 1];
                _blocks.RemoveAt(_blocks.Count - 1);
                DestroyOwned(last);
            }

            while (_blocks.Count < logicalLayerCount)
            {
                CreateBlock(_blocks.Count, useDirt);
            }

            _usesDirt = useDirt;
            LayerCount = logicalLayerCount;
            RebuildComponentLists();
            RefreshBounds();
            Changed?.Invoke(this);
        }

        public void Clear()
        {
            ApplyLogicalLayerCount(0);
        }

        private void CreateBlock(int layer, bool useDirt)
        {
            var block = new GameObject(string.Format("Block-{0}", layer));
            block.transform.SetParent(transform, false);
            block.transform.localPosition = new Vector3(0f, layer * _layerSpacing, 0f);

            var prefab = useDirt ? _dirtPrefab : _grassPrefab;
            var visual = Instantiate(prefab, block.transform, false);
            visual.name = useDirt ? "Visual-Dirt" : "Visual-Grass";
            var meshFilter = visual.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter != null)
            {
                var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }

            _blocks.Add(block);
        }

        private void RebuildComponentLists()
        {
            _renderers.Clear();
            _colliders.Clear();
            for (var index = 0; index < _blocks.Count; index++)
            {
                var block = _blocks[index];
                foreach (var collider in block.GetComponentsInChildren<Collider>(true))
                {
                    _colliders.Add(collider);
                }

                _renderers.AddRange(block.GetComponentsInChildren<Renderer>(true));
            }
        }

        private void RefreshBounds()
        {
            if (_blocks.Count == 0)
            {
                TopBounds = new Bounds(transform.position, Vector3.zero);
                EnsureOccupantAnchor();
                _occupantAnchor.position = transform.position;
                return;
            }

            var top = _blocks[_blocks.Count - 1];
            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var renderer in top.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            foreach (var collider in top.GetComponentsInChildren<Collider>(true))
            {
                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            TopBounds = hasBounds ? bounds : new Bounds(top.transform.position, Vector3.zero);
            EnsureOccupantAnchor();
            _occupantAnchor.position = new Vector3(
                TopBounds.center.x,
                TopBounds.max.y,
                TopBounds.center.z);
        }

        private void EnsureOccupantAnchor()
        {
            if (_occupantAnchor != null)
            {
                return;
            }

            var anchor = new GameObject("OccupantAnchor");
            anchor.transform.SetParent(transform, true);
            _occupantAnchor = anchor.transform;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (value is GameObject gameObject)
                {
                    gameObject.SetActive(false);
                }

                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }
    }
}
