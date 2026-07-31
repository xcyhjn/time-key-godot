using System;
using System.Collections;
using System.Collections.Generic;
using TimeKey.Domain;
using TimeKey.Infrastructure;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation
{
    public sealed class VerticalSliceController : MonoBehaviour
    {
        public const string LightingCardId = "lighting";
        public const string TargetId = "target-01";
        public const int FixtureSeed = 731;
        public const float HexBlockHeight = 0.32f;

        [SerializeField] private TextAsset lightingFixture = null;

        private readonly List<Button> _timelineButtons = new List<Button>(36);
        private readonly Dictionary<HexCoord, BoardTileView> _tiles = new Dictionary<HexCoord, BoardTileView>();
        private GameObject _generatedRoot;
        private CardDefinition _lightingCard;
        private CombatSliceState _state;
        private TimelineGrid _timeline;
        private CardDefinition _selectedCard;
        private string _selectedTargetId;
        private TimelineCell? _playerCell;
        private ResolutionSnapshot _lastSnapshot;
        private Text _statusText;
        private Text _targetText;
        private Button _cardButton;
        private Button _resolveButton;
        private Renderer _targetRenderer;
        private GameObject _targetObject;
        private Material _targetMaterial;
        private Material _groundMaterial;
        private Mesh _hexMesh;
        private Sprite _centerAltarSprite;
        private GameObject _grassTilePrefab;
        private GameObject _dirtTilePrefab;
        private BoardTileView _selectedTile;

        public int CurrentTargetHp => _state == null ? 0 : _state.TargetHp;

        public bool EnemyIntentResolved => _lastSnapshot != null && _lastSnapshot.EnemyIntentResolved;

        public int TimelineSlotCount => _timelineButtons.Count;

        public int BoardTileCount => _tiles.Count;

        public HexCoord? SelectedTile => _selectedTile == null ? (HexCoord?)null : _selectedTile.Coordinate;

        public Vector3 TargetWorldPosition => _targetObject == null ? Vector3.zero : _targetObject.transform.position;

        public Camera SceneCamera { get; private set; }

        public Canvas SceneCanvas { get; private set; }

        public BoardOrbitCameraController BoardCamera { get; private set; }

        private void Awake()
        {
            BuildSceneGraph();
        }

        private void Update()
        {
            if (_generatedRoot == null || BoardCamera == null || BoardCamera.IsManipulating)
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                TrySelectWorldAtScreenPoint(Input.mousePosition);
            }
        }

        private IEnumerator Start()
        {
            if (!HasCommandLineArgument("-timekeySmokeQuit"))
            {
                yield break;
            }

            yield return null;

            try
            {
                var arranged = SelectCard(LightingCardId) &&
                    SelectTarget(TargetId) &&
                    TryPlaceSelected(0, 0);
                var snapshot = arranged ? ResolveTimeline() : null;
                if (snapshot == null || snapshot.TargetHpAfter != 0 || !snapshot.EnemyIntentResolved)
                {
                    throw new InvalidOperationException("The Player smoke path did not satisfy the frozen slice contract.");
                }

                Debug.Log("TIMEKEY_PLAYER_SMOKE_PASS");
                Application.Quit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Application.Quit(1);
            }
        }

        public void BuildSceneGraph()
        {
            if (_generatedRoot != null)
            {
                return;
            }

            if (lightingFixture == null)
            {
                throw new InvalidOperationException("The lighting card fixture is not assigned.");
            }

            _lightingCard = CardJsonAdapter.Parse(lightingFixture.text);
            _state = new CombatSliceState(TargetId, 10, FixtureSeed);
            _timeline = new TimelineGrid();

            _generatedRoot = new GameObject("GeneratedSlice");
            _generatedRoot.transform.SetParent(transform, false);

            BuildWorld();
            BuildInterface();
            PlaceEnemyIntent();
            SetStatus("Select LIGHTING, click the red target, then choose a timeline slot.");
        }

        public bool SelectCard(string stableId)
        {
            EnsureBuilt();
            if (!string.Equals(stableId, _lightingCard.StableId, StringComparison.Ordinal))
            {
                return false;
            }

            _selectedCard = _lightingCard;
            SetButtonColor(_cardButton, new Color(0.18f, 0.72f, 0.78f, 1f));
            SetStatus("LIGHTING selected. Click the red target in the 3D battlefield.");
            return true;
        }

        public bool SelectTarget(string targetId)
        {
            EnsureBuilt();
            if (_selectedCard == null || !string.Equals(targetId, TargetId, StringComparison.Ordinal))
            {
                return false;
            }

            _selectedTargetId = targetId;
            _targetText.text = "TARGET 01  |  HP 10 / 10  |  LOCKED";
            SetStatus("Target locked. Place LIGHTING in an open timeline slot.");
            return true;
        }

        public bool SelectTile(HexCoord coordinate)
        {
            EnsureBuilt();
            if (!_tiles.TryGetValue(coordinate, out var tile))
            {
                return false;
            }

            if (_selectedTile != null)
            {
                _selectedTile.SetSelected(false);
            }

            _selectedTile = tile;
            _selectedTile.SetSelected(true);
            SetStatus(string.Format("HEX {0},{1} selected.", coordinate.Q, coordinate.R));
            return true;
        }

        public void SetBoardView(float yaw, float pitch = 48f, float distance = 15.5f)
        {
            EnsureBuilt();
            BoardCamera.SetView(yaw, pitch, distance, new Vector3(0f, 0.45f, 0f), true);
            foreach (var billboard in _generatedRoot.GetComponentsInChildren<CameraFacingBillboard>())
            {
                billboard.FaceCamera();
            }
        }

        public bool TrySelectWorldAtScreenPoint(Vector2 screenPoint)
        {
            EnsureBuilt();
            if (IsScreenPointOverInterface(screenPoint))
            {
                return false;
            }

            var ray = SceneCamera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out var hit, SceneCamera.farClipPlane))
            {
                return false;
            }

            var target = hit.collider.GetComponentInParent<WorldTargetView>();
            if (target != null)
            {
                return SelectTarget(target.TargetId);
            }

            var tile = hit.collider.GetComponentInParent<BoardTileView>();
            return tile != null && SelectTile(tile.Coordinate);
        }

        public bool IsScreenPointOverInterface(Vector2 screenPoint)
        {
            if (SceneCanvas == null)
            {
                return false;
            }

            foreach (var graphic in SceneCanvas.GetComponentsInChildren<Graphic>(false))
            {
                if (graphic.raycastTarget &&
                    graphic.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        graphic.rectTransform,
                        screenPoint,
                        SceneCanvas.worldCamera))
                {
                    return true;
                }
            }

            if (EventSystem.current == null)
            {
                return false;
            }

            var pointer = new PointerEventData(EventSystem.current) { position = screenPoint };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            foreach (var result in results)
            {
                var canvas = result.gameObject.GetComponentInParent<Canvas>();
                if (canvas == SceneCanvas)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryPlaceSelected(int column, int row)
        {
            EnsureBuilt();
            if (_selectedCard == null || string.IsNullOrEmpty(_selectedTargetId) || _playerCell.HasValue)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            var action = TimelineAction.FromCard(_selectedCard, _selectedTargetId, cell);
            if (!_timeline.TryPlace(action))
            {
                SetStatus("That timeline slot is occupied or outside the 12 x 3 grid.");
                return false;
            }

            _playerCell = cell;
            SetTimelineCell(cell, "LIGHT", new Color(0.16f, 0.74f, 0.82f, 1f));
            _resolveButton.interactable = true;
            SetStatus("LIGHTING placed. Resolve to apply damage before the enemy intent.");
            return true;
        }

        public ResolutionSnapshot ResolveTimeline()
        {
            EnsureBuilt();
            if (!_playerCell.HasValue)
            {
                throw new InvalidOperationException("Place the player action before resolving.");
            }

            _lastSnapshot = _timeline.Resolve(_state);
            _targetText.text = "TARGET 01  |  HP 0 / 10  |  DISABLED";
            _targetMaterial.color = new Color(0.24f, 0.68f, 0.46f, 1f);
            _resolveButton.interactable = false;
            SetStatus("RESOLVED  |  LIGHTING: 10 -> 0 HP  |  ENEMY INTENT: PROCESSED");
            return _lastSnapshot;
        }

        private void BuildWorld()
        {
            var cameraObject = new GameObject("SliceCamera");
            cameraObject.transform.SetParent(_generatedRoot.transform, false);
            SceneCamera = cameraObject.AddComponent<Camera>();
            SceneCamera.orthographic = false;
            SceneCamera.fieldOfView = 38f;
            SceneCamera.clearFlags = CameraClearFlags.SolidColor;
            SceneCamera.backgroundColor = new Color(0.035f, 0.047f, 0.055f, 1f);
            SceneCamera.nearClipPlane = 0.1f;
            SceneCamera.farClipPlane = 100f;
            BoardCamera = cameraObject.AddComponent<BoardOrbitCameraController>();
            BoardCamera.Initialize(SceneCamera, new Vector3(0f, 0.45f, 0f), 32f, 48f, 15.5f);

            var lightObject = new GameObject("KeyLight");
            lightObject.transform.SetParent(_generatedRoot.transform, false);
            var keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.92f, 0.78f, 1f);
            keyLight.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var fillObject = new GameObject("FillLight");
            fillObject.transform.SetParent(_generatedRoot.transform, false);
            var fillLight = fillObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.35f, 0.66f, 0.92f, 1f);
            fillLight.intensity = 0.65f;
            fillObject.transform.rotation = Quaternion.Euler(36f, 145f, 0f);

            _hexMesh = HexMeshFactory.Create(0.94f, HexBlockHeight);
            _groundMaterial = CreateUnlitMaterial(new Color(0.055f, 0.07f, 0.065f, 1f));
            _grassTilePrefab = LoadTilePrefab("Art/Battle/Models/HexTile_Grass");
            _dirtTilePrefab = LoadTilePrefab("Art/Battle/Models/HexTile_Dirt");
            _centerAltarSprite = CreateOriginalSprite("Art/Battle/center_altar");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattlefieldGround";
            ground.transform.SetParent(_generatedRoot.transform, false);
            ground.transform.position = new Vector3(0f, -0.04f, 0f);
            ground.transform.localScale = new Vector3(2.1f, 1f, 2.1f);
            ground.GetComponent<Renderer>().sharedMaterial = _groundMaterial;

            for (var q = -2; q <= 2; q++)
            {
                var minimumR = Mathf.Max(-2, -q - 2);
                var maximumR = Mathf.Min(2, -q + 2);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    var elevation = (q == 1 && r == 0) || (q == -1 && r == 1) ? 1 : 0;
                    CreateTile(new HexCoord(q, r), elevation);
                }
            }

            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _targetObject.name = "Target-target-01";
            _targetObject.transform.SetParent(_generatedRoot.transform, false);
            _targetObject.transform.position = HexToWorld(new HexCoord(1, 0), 0f) +
                new Vector3(0f, (2f * HexBlockHeight) + 0.10f, 0f);
            _targetObject.transform.localScale = new Vector3(0.58f, 0.10f, 0.58f);
            _targetMaterial = CreateMaterial(new Color(0.86f, 0.24f, 0.24f, 1f));
            _targetRenderer = _targetObject.GetComponent<Renderer>();
            _targetRenderer.sharedMaterial = _targetMaterial;
            _targetObject.AddComponent<WorldTargetView>().Initialize(TargetId);

            var targetArt = new GameObject("OriginalArt-center_altar", typeof(SpriteRenderer), typeof(BoxCollider));
            targetArt.transform.SetParent(_generatedRoot.transform, false);
            targetArt.transform.position = _targetObject.transform.position + new Vector3(0f, 0.85f, 0f);
            targetArt.transform.localScale = Vector3.one * 0.62f;
            var targetSpriteRenderer = targetArt.GetComponent<SpriteRenderer>();
            targetSpriteRenderer.sprite = _centerAltarSprite;
            targetSpriteRenderer.color = Color.white;
            targetSpriteRenderer.sortingOrder = 100;
            targetSpriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            targetArt.GetComponent<BoxCollider>().size = new Vector3(2.25f, 2.25f, 0.16f);
            targetArt.AddComponent<WorldTargetView>().Initialize(TargetId);
            targetArt.AddComponent<CameraFacingBillboard>().Initialize(SceneCamera);
        }

        private void BuildInterface()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(_generatedRoot.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasObject = new GameObject("SliceCanvas");
            canvasObject.transform.SetParent(_generatedRoot.transform, false);
            SceneCanvas = canvasObject.AddComponent<Canvas>();
            SceneCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            SceneCanvas.worldCamera = SceneCamera;
            SceneCanvas.planeDistance = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var header = CreatePanel(
                SceneCanvas.transform,
                "Header",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(24f, -104f),
                new Vector2(-24f, -24f),
                new Color(0.06f, 0.08f, 0.09f, 1f));
            CreateText(header.transform, "Title", "TIME KEY  /  COMBAT BOARD", 28, TextAnchor.MiddleLeft,
                new Vector2(22f, 8f), new Vector2(-22f, -38f));
            _statusText = CreateText(header.transform, "Status", string.Empty, 20, TextAnchor.MiddleLeft,
                new Vector2(22f, 42f), new Vector2(-22f, -8f));

            var timelinePanel = CreatePanel(
                SceneCanvas.transform,
                "Timeline",
                new Vector2(0.18f, 0.73f),
                new Vector2(0.82f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.09f, 0.10f, 1f));
            var grid = timelinePanel.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(20, 20, 22, 18);
            grid.cellSize = new Vector2(88f, 44f);
            grid.spacing = new Vector2(4f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = TimelineGrid.DefaultWidth;

            for (var row = 0; row < TimelineGrid.DefaultHeight; row++)
            {
                for (var column = 0; column < TimelineGrid.DefaultWidth; column++)
                {
                    var capturedColumn = column;
                    var capturedRow = row;
                    var button = CreateButton(
                        timelinePanel.transform,
                        string.Format("Slot-{0}-{1}", column, row),
                        string.Format("{0:00}", column + 1),
                        new Color(0.16f, 0.19f, 0.20f, 1f));
                    button.onClick.AddListener(() => TryPlaceSelected(capturedColumn, capturedRow));
                    _timelineButtons.Add(button);
                }
            }

            var cardPanel = CreatePanel(
                SceneCanvas.transform,
                "CardPanel",
                Vector2.zero,
                Vector2.zero,
                new Vector2(24f, 24f),
                new Vector2(364f, 224f),
                new Color(0.08f, 0.10f, 0.11f, 0.96f));
            CreateText(cardPanel.transform, "CardTitle", "LIGHTING", 30, TextAnchor.MiddleLeft,
                new Vector2(20f, 112f), new Vector2(-20f, -18f));
            CreateText(cardPanel.transform, "CardBody", "DAMAGE 100\nRANGE 3 HEXES\nSHAPE 1 CELL", 19, TextAnchor.UpperLeft,
                new Vector2(20f, 48f), new Vector2(-20f, -76f));
            _cardButton = CreateButton(cardPanel.transform, "SelectCard", "SELECT CARD", new Color(0.22f, 0.34f, 0.36f, 1f));
            SetRect(_cardButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
                new Vector2(20f, 18f), new Vector2(320f, 64f));
            _cardButton.onClick.AddListener(() => SelectCard(LightingCardId));

            var detailPanel = CreatePanel(
                SceneCanvas.transform,
                "DetailPanel",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-414f, 24f),
                new Vector2(-24f, 224f),
                new Color(0.08f, 0.10f, 0.11f, 0.96f));
            _targetText = CreateText(detailPanel.transform, "TargetStatus", "TARGET 01  |  HP 10 / 10", 20,
                TextAnchor.MiddleLeft, new Vector2(20f, 112f), new Vector2(-20f, -18f));
            CreateText(detailPanel.transform, "Intent", "ENEMY INTENT  /  SLOT 03-B", 18,
                TextAnchor.MiddleLeft, new Vector2(20f, 72f), new Vector2(-20f, -60f));
            _resolveButton = CreateButton(detailPanel.transform, "Resolve", "RESOLVE TIMELINE", new Color(0.70f, 0.25f, 0.20f, 1f));
            SetRect(_resolveButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
                new Vector2(20f, 18f), new Vector2(370f, 64f));
            _resolveButton.interactable = false;
            _resolveButton.onClick.AddListener(() => ResolveTimeline());
        }

        private void CreateTile(HexCoord coordinate, int elevation)
        {
            var tile = new GameObject(string.Format("Hex-{0}-{1}", coordinate.Q, coordinate.R));
            tile.transform.SetParent(_generatedRoot.transform, false);
            tile.transform.position = HexToWorld(coordinate, 0f);
            var renderers = new List<Renderer>((elevation + 1) * 4);
            var tilePrefab = elevation > 0 ? _dirtTilePrefab : _grassTilePrefab;
            var variantName = elevation > 0 ? "Dirt" : "Grass";

            for (var layer = 0; layer <= elevation; layer++)
            {
                var block = new GameObject(string.Format("Block-{0}", layer));
                block.transform.SetParent(tile.transform, false);
                block.transform.localPosition = new Vector3(0f, layer * HexBlockHeight, 0f);
                var collider = block.AddComponent<MeshCollider>();
                collider.sharedMesh = _hexMesh;
                var visual = Instantiate(tilePrefab, block.transform, false);
                visual.name = "Visual-" + variantName;
                renderers.AddRange(visual.GetComponentsInChildren<Renderer>(true));
            }

            var tileView = tile.AddComponent<BoardTileView>();
            tileView.Initialize(coordinate, renderers.ToArray());
            _tiles.Add(coordinate, tileView);
        }

        private void PlaceEnemyIntent()
        {
            var origin = new TimelineCell(2, 1);
            var intent = new TimelineAction(
                TimelineActorKind.Enemy,
                "enemy-intent",
                TargetId,
                origin,
                new[] { new TimelineCell(0, 0) },
                0);
            if (!_timeline.TryPlace(intent))
            {
                throw new InvalidOperationException("The frozen enemy intent could not be placed.");
            }

            SetTimelineCell(origin, "INTENT", new Color(0.78f, 0.27f, 0.25f, 1f));
        }

        private void SetTimelineCell(TimelineCell cell, string label, Color color)
        {
            var index = (cell.Y * TimelineGrid.DefaultWidth) + cell.X;
            var button = _timelineButtons[index];
            button.GetComponentInChildren<Text>().text = label;
            SetButtonColor(button, color);
        }

        private static Vector3 HexToWorld(HexCoord coordinate, float elevation)
        {
            const float radius = 1f;
            var x = 1.5f * radius * coordinate.Q;
            var z = Mathf.Sqrt(3f) * radius * (coordinate.R + (coordinate.Q / 2f));
            return new Vector3(x, elevation, z);
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No compatible lit shader is available.");
            }

            var material = new Material(shader) { color = color };
            material.enableInstancing = true;
            return material;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No compatible unlit shader is available.");
            }

            var material = new Material(shader) { color = color };
            material.enableInstancing = true;
            return material;
        }

        private static Sprite CreateOriginalSprite(string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Original project art is missing: " + resourcePath);
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + "-runtime-sprite";
            return sprite;
        }

        private static GameObject LoadTilePrefab(string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Hex tile model is missing: " + resourcePath);
            }

            return prefab;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.94f, 0.90f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            SetButtonColor(button, color);
            CreateText(buttonObject.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            button.targetGraphic.color = color;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.90f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.86f, 1f);
            colors.disabledColor = new Color(0.42f, 0.44f, 0.45f, 0.75f);
            button.colors = colors;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void SetStatus(string value)
        {
            _statusText.text = value;
        }

        private void EnsureBuilt()
        {
            if (_generatedRoot == null)
            {
                BuildSceneGraph();
            }
        }

        private void OnDestroy()
        {
            DestroyOwnedObject(_targetMaterial);
            DestroyOwnedObject(_groundMaterial);
            DestroyOwnedObject(_hexMesh);
            DestroyOwnedObject(_centerAltarSprite);
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private static bool HasCommandLineArgument(string expected)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
