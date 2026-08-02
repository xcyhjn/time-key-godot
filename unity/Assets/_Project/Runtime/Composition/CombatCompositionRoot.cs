using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Domain.Intents;
using TimeKey.Infrastructure.Cards;
using TimeKey.Infrastructure.Effects;
using TimeKey.Presentation;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Localization;
using UnityEngine;

namespace TimeKey.Composition
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class CombatCompositionRoot : MonoBehaviour
    {
        [SerializeField] private VerticalSliceController controller = null;
        [SerializeField] private CombatPresentationBinding presentationBinding = null;
        [SerializeField] private List<TextAsset> cardFixtures = new List<TextAsset>(7);
        [SerializeField] private UnityCombatTraceSink traceSink = null;

        private readonly List<Sprite> _ownedSprites = new List<Sprite>();
        private CombatApplicationSession _session;
        private bool _initialized;

        public int CardCount { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            ValidateSerializedReferences();
            var documents = new List<string>(cardFixtures.Count);
            for (var index = 0; index < cardFixtures.Count; index++)
            {
                documents.Add(cardFixtures[index].text);
            }

            var catalog = CardContentCatalog.FromJson(documents);
            var registrations = CardEffectRegistrationCatalog.CreateVerticalSlice();
            var sprites = LoadArtwork(catalog.Entries);
            presentationBinding.ConfigureCards(
                CreateCardModels(catalog.Cards, sprites, registrations));

            var board = CreateBoard();
            var state = new CombatSliceState(
                VerticalSliceController.TargetId,
                10,
                VerticalSliceController.FixtureSeed,
                board);
            var timeline = new TimelineGrid();
            var battleFlow = new BattleFlowNextTurnHook(
                DeckState.CreateStarter(
                    VerticalSliceController.FixtureSeed,
                    "combat-vertical-slice"),
                new BattleRoundLedger(),
                new BattleSettlementState(
                    "combat-vertical-slice",
                    VerticalSliceController.FixtureSeed,
                    new BattleRewardEntry(
                        "combat-vertical-slice/acquire-card",
                        BattleRewardKind.Acquire,
                        "获得卡牌")));
            _session = new CombatApplicationSession(
                catalog,
                state,
                timeline,
                traceSink: traceSink,
                actionDisplayCatalog: CombatChineseActionDisplayCatalog.Instance,
                enemyIntentSourceCatalog: VerticalSliceEnemyIntentSourceCatalog.Instance,
                battleFlow: battleFlow);
            controller.Initialize(
                _session,
                state,
                timeline,
                Array.Empty<TimelineAction>());
            CardCount = catalog.Cards.Count;
            _initialized = true;
        }

        private static IReadOnlyList<CardViewModel> CreateCardModels(
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyDictionary<string, Sprite> sprites,
            CardEffectRegistrationCatalog registrations)
        {
            var result = new List<CardViewModel>(cards.Count);
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                result.Add(new CardViewModel(
                    card.StableId,
                    sprites[card.StableId],
                    false,
                    registrations.Supports(card),
                    CombatChineseText.GetCardName(card.StableId),
                    CombatChineseText.GetCardEffectDescription(card),
                    CombatChineseText.GetCardPlacementDescription(card)));
            }

            return result;
        }

        private static CombatBoardState CreateBoard()
        {
            var board = new CombatBoardState();
            for (var q = -2; q <= 2; q++)
            {
                var minimumR = Math.Max(-2, -q - 2);
                var maximumR = Math.Min(2, -q + 2);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    var elevated = (q == 1 && r == 0) || (q == -1 && r == 1);
                    board.AddTile(new HexCoord(q, r), elevated ? 2 : 1);
                }
            }

            return board;
        }

        private sealed class VerticalSliceEnemyIntentSourceCatalog :
            IEnemyIntentSourceCatalog
        {
            public static VerticalSliceEnemyIntentSourceCatalog Instance { get; } =
                new VerticalSliceEnemyIntentSourceCatalog();

            private VerticalSliceEnemyIntentSourceCatalog()
            {
            }

            public IReadOnlyList<EnemyIntentSourceSnapshot> CaptureSources(
                CombatSliceState state)
            {
                if (state == null)
                {
                    throw new ArgumentNullException(nameof(state));
                }

                var sources = new List<EnemyIntentSourceSnapshot>();
                var occupants = state.CaptureOccupants();
                for (var index = 0; index < occupants.Count; index++)
                {
                    var occupant = occupants[index];
                    if (occupant.Attitude != CombatAttitude.Enemy)
                    {
                        continue;
                    }

                    sources.Add(new EnemyIntentSourceSnapshot(
                        occupant.RuntimeId,
                        occupant.Coordinate,
                        occupant.Kind,
                        occupant.IsAlive,
                        occupant.IsAlive,
                        "enemy-intent",
                        0,
                        new EnemyIntentTargetSnapshot(
                            occupant.RuntimeId,
                            occupant.Coordinate,
                            requiresOccupant: true,
                            occupantRuntimeId: occupant.RuntimeId,
                            requiredAttitude: CombatAttitude.Enemy),
                        new[]
                        {
                            new TimelineCell(1, 0),
                            new TimelineCell(2, 0)
                        },
                        new EnemyIntentEffectSnapshot(
                            "enemy-intent-effect",
                            sourceCommand: null,
                            rangeOffsets: new[] { new HexCoord(0, 0) }),
                        new EnemyIntentDisplayData(
                            "敌方意图",
                            "源命令暂不支持，本轮不产生效果",
                            "enemy-intent",
                            "来源：" + CombatChineseText.GetEntityName(occupant.RuntimeId),
                            "目标：自身")));
                }

                return sources;
            }
        }

        private IReadOnlyDictionary<string, Sprite> LoadArtwork(
            IReadOnlyList<CardContentEntry> entries)
        {
            var result = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var texture = Resources.Load<Texture2D>(entry.ArtworkResourcePath);
                if (texture == null)
                {
                    var importedSprite = Resources.Load<Sprite>(entry.ArtworkResourcePath);
                    texture = importedSprite == null ? null : importedSprite.texture;
                }

                if (texture == null)
                {
                    throw new InvalidOperationException(
                        "Card artwork is missing: " + entry.FrontImage + ".");
                }

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = entry.StableId + "-runtime-sprite";
                _ownedSprites.Add(sprite);
                result.Add(entry.StableId, sprite);
            }

            return result;
        }

        private void ValidateSerializedReferences()
        {
            if (controller == null)
            {
                throw new InvalidOperationException("Missing serialized reference: controller.");
            }

            if (traceSink == null)
            {
                throw new InvalidOperationException("Missing serialized reference: traceSink.");
            }

            if (presentationBinding == null)
            {
                throw new InvalidOperationException("Missing serialized reference: presentationBinding.");
            }

            if (cardFixtures == null || cardFixtures.Count == 0)
            {
                throw new InvalidOperationException(
                    "cardFixtures must contain at least one serialized card asset.");
            }

            for (var index = 0; index < cardFixtures.Count; index++)
            {
                if (cardFixtures[index] == null)
                {
                    throw new InvalidOperationException(
                        "cardFixtures contains a missing reference at index " + index + ".");
                }
            }
        }

        private void OnDestroy()
        {
            _session?.Dispose();
            for (var index = 0; index < _ownedSprites.Count; index++)
            {
                if (_ownedSprites[index] == null)
                {
                    continue;
                }

                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(_ownedSprites[index]);
                }
                else
                {
                    DestroyImmediate(_ownedSprites[index]);
                }
            }
        }
    }
}
