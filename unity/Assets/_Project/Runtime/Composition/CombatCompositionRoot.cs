using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Infrastructure.Cards;
using TimeKey.Infrastructure.Effects;
using TimeKey.Presentation;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
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

            var state = new CombatSliceState(
                VerticalSliceController.TargetId,
                10,
                VerticalSliceController.FixtureSeed,
                new CombatBoardState());
            var timeline = new TimelineGrid();
            var enemyIntent = CreateEnemyIntent();
            _session = new CombatApplicationSession(
                catalog,
                state,
                timeline,
                new[] { enemyIntent },
                traceSink);
            controller.Initialize(_session, state, timeline, new[] { enemyIntent });
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
                    registrations.Supports(card)));
            }

            return result;
        }

        private static TimelineAction CreateEnemyIntent()
        {
            return new TimelineAction(
                TimelineActorKind.Enemy,
                "enemy-intent",
                VerticalSliceController.TargetId,
                new TimelineCell(2, 1),
                new[] { new TimelineCell(0, 0) },
                0);
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
