using System;
using TimeKey.Application;
using TimeKey.Presentation.Cards;
using UnityEngine;

namespace TimeKey.Presentation.Tooltips
{
    [DisallowMultipleComponent]
    public sealed class CombatInteractionOverlayPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform frameHost = null;
        [SerializeField] private CardEffectFrame framePrefab = null;
        private CardEffectFrame _frame;

        public CardEffectFrame CurrentFrame => _frame;

        public void ShowCard(CardViewModel card)
        {
            EnsureFrame().ShowCard(card);
        }

        public void ShowAction(TimelineActionPresentationSnapshot action)
        {
            EnsureFrame().ShowAction(action);
        }

        public void Clear()
        {
            if (_frame != null)
            {
                _frame.Hide();
            }
        }

        private CardEffectFrame EnsureFrame()
        {
            if (_frame != null)
            {
                return _frame;
            }

            if (frameHost == null || framePrefab == null)
            {
                throw new InvalidOperationException(name + " is missing serialized effect-frame references.");
            }

            _frame = Instantiate(framePrefab, frameHost, false);
            _frame.name = "ActiveEffectFrame";
            return _frame;
        }

        private void OnDisable()
        {
            Clear();
        }
    }
}
