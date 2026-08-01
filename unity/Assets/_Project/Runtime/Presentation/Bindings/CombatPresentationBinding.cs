using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Terrain;
using UnityEngine;

namespace TimeKey.Presentation.Bindings
{
    [DisallowMultipleComponent]
    public sealed class CombatPresentationBinding : MonoBehaviour
    {
        [SerializeField] private CardHandPresenter cardHandPresenter = null;
        [SerializeField] private BoardRangePresenter boardRangePresenter = null;
        [SerializeField] private TimelinePresenter timelinePresenter = null;
        [SerializeField] private CombatHudPresenter hudPresenter = null;
        [SerializeField] private CombatOccupantPresenter occupantPresenter = null;

        private bool _isBound;

        public event Action<string> CardSelected;
        public event Action<string> CardCancelled;
        public event Action<string, Vector2, CardDragPhase> CardDragChanged;
        public event Action<TimelineCell> TimelineSelected;
        public event Action<TimelineCell> TimelinePreviewRequested;
        public event Action TimelinePreviewCleared;
        public event Action ResolveRequested;

        public bool IsBound => _isBound;

        public int TimelineCellCount => timelinePresenter == null ? 0 : timelinePresenter.CellCount;

        public CombatOccupantPresenter OccupantPresenter => occupantPresenter;

        public void ConfigureCards(IReadOnlyList<CardViewModel> cards)
        {
            ValidateDependencies();
            cardHandPresenter.ConfigureCards(cards);
        }

        public void Bind()
        {
            ValidateDependencies();
            if (_isBound)
            {
                return;
            }

            cardHandPresenter.Bind();
            timelinePresenter.Bind();
            hudPresenter.Bind();

            cardHandPresenter.CardSelected += HandleCardSelected;
            cardHandPresenter.CardCancelled += HandleCardCancelled;
            cardHandPresenter.CardDragChanged += HandleCardDragChanged;
            timelinePresenter.TimelineSelected += HandleTimelineSelected;
            timelinePresenter.TimelinePreviewRequested += HandleTimelinePreviewRequested;
            timelinePresenter.TimelinePreviewCleared += HandleTimelinePreviewCleared;
            hudPresenter.ResolveRequested += HandleResolveRequested;
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            cardHandPresenter.CardSelected -= HandleCardSelected;
            cardHandPresenter.CardCancelled -= HandleCardCancelled;
            cardHandPresenter.CardDragChanged -= HandleCardDragChanged;
            timelinePresenter.TimelineSelected -= HandleTimelineSelected;
            timelinePresenter.TimelinePreviewRequested -= HandleTimelinePreviewRequested;
            timelinePresenter.TimelinePreviewCleared -= HandleTimelinePreviewCleared;
            hudPresenter.ResolveRequested -= HandleResolveRequested;

            cardHandPresenter.Unbind();
            timelinePresenter.Unbind();
            hudPresenter.Unbind();
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            cardHandPresenter.Refresh(state);
            boardRangePresenter.Refresh(state);
            timelinePresenter.Refresh(state);
            hudPresenter.Refresh(state);
        }

        public void RenderTimelineAction(
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            string label,
            Color color)
        {
            ValidateDependencies();
            timelinePresenter.RenderAction(origin, shape, label, color);
        }

        public void ClearTimelinePreview()
        {
            ValidateDependencies();
            timelinePresenter.ClearPreview();
        }

        public void RegisterOccupantColumn(HexCoord coordinate, HexTileColumn column)
        {
            ValidateDependencies();
            occupantPresenter.RegisterColumn(coordinate, column);
        }

        public void RegisterExistingOccupant(
            CombatOccupantSnapshot occupant,
            CombatOccupantView view)
        {
            ValidateDependencies();
            occupantPresenter.RegisterExisting(occupant, view);
        }

        public void ApplyOccupantEffects(IReadOnlyList<OccupantEffectResult> results)
        {
            ValidateDependencies();
            occupantPresenter.Apply(results);
        }

        private void OnEnable()
        {
            if (cardHandPresenter != null &&
                boardRangePresenter != null &&
                timelinePresenter != null &&
                hudPresenter != null &&
                occupantPresenter != null)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void ValidateDependencies()
        {
            if (cardHandPresenter == null)
            {
                throw MissingReference(nameof(cardHandPresenter));
            }

            if (boardRangePresenter == null)
            {
                throw MissingReference(nameof(boardRangePresenter));
            }

            if (timelinePresenter == null)
            {
                throw MissingReference(nameof(timelinePresenter));
            }

            if (hudPresenter == null)
            {
                throw MissingReference(nameof(hudPresenter));
            }

            if (occupantPresenter == null)
            {
                throw MissingReference(nameof(occupantPresenter));
            }
        }

        private InvalidOperationException MissingReference(string fieldName)
        {
            return new InvalidOperationException(
                name + " is missing serialized " + fieldName + " reference.");
        }

        private void HandleCardSelected(string stableId)
        {
            CardSelected?.Invoke(stableId);
        }

        private void HandleCardCancelled(string stableId)
        {
            CardCancelled?.Invoke(stableId);
        }

        private void HandleCardDragChanged(
            string stableId,
            Vector2 pointerPosition,
            CardDragPhase phase)
        {
            CardDragChanged?.Invoke(stableId, pointerPosition, phase);
        }

        private void HandleTimelineSelected(TimelineCell coordinate)
        {
            TimelineSelected?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewRequested(TimelineCell coordinate)
        {
            TimelinePreviewRequested?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewCleared()
        {
            TimelinePreviewCleared?.Invoke();
        }

        private void HandleResolveRequested()
        {
            ResolveRequested?.Invoke();
        }
    }
}
