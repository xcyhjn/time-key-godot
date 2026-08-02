using System;
using System.Collections.Generic;

namespace TimeKey.Domain.Deck
{
    public sealed class DeckState
    {
        public const int HandLimit = 7;
        public const int FormalDrawRequest = 5;

        private readonly HashSet<DeckCommandId> _completedCommands =
            new HashSet<DeckCommandId>();
        private readonly List<CardInstance> _discardPile = new List<CardInstance>();
        private readonly List<CardInstance> _drawPile;
        private readonly List<CardInstance> _hand = new List<CardInstance>();
        private readonly DeterministicDeckRandom _random;

        public DeckState(
            IReadOnlyList<CardInstance> orderedCards,
            ulong seed,
            bool shuffleInitially)
        {
            if (orderedCards == null)
            {
                throw new ArgumentNullException(nameof(orderedCards));
            }

            _drawPile = CopyAndValidateCards(orderedCards);
            _random = new DeterministicDeckRandom(seed);
            if (shuffleInitially)
            {
                _random.Shuffle(_drawPile);
            }
        }

        public static DeckState CreateStarter(
            ulong seed,
            string identityScope)
        {
            return new DeckState(
                StarterDeck.CreateInstances(identityScope),
                seed,
                true);
        }

        public DeckSnapshot Snapshot()
        {
            return new DeckSnapshot(_drawPile, _hand, _discardPile);
        }

        public DeckMoveResult Draw(DeckCommandId commandId, int requestedCount)
        {
            var before = Counts();
            var validationFailure = Validate(commandId, requestedCount);
            if (validationFailure != DeckOperationReason.None)
            {
                return Failure(validationFailure, requestedCount, before);
            }

            var capacity = HandLimit - _hand.Count;
            if (capacity <= 0)
            {
                return Failure(DeckOperationReason.HandLimitReached, requestedCount, before);
            }

            var targetCount = Math.Min(requestedCount, capacity);
            var movedIds = new List<CardInstanceId>(targetCount);
            var shuffle = DeckShuffleInfo.None();
            while (movedIds.Count < targetCount)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                    {
                        break;
                    }

                    shuffle = RecycleDiscardIntoDrawPile();
                }

                var topIndex = _drawPile.Count - 1;
                var card = _drawPile[topIndex];
                _drawPile.RemoveAt(topIndex);
                _hand.Add(card);
                movedIds.Add(card.InstanceId);
            }

            if (movedIds.Count == 0)
            {
                return Failure(DeckOperationReason.Exhausted, requestedCount, before);
            }

            _completedCommands.Add(commandId);
            var reason = ResolveDrawReason(requestedCount, capacity, movedIds.Count);
            return new DeckMoveResult(
                true,
                reason,
                requestedCount,
                before,
                Counts(),
                movedIds,
                shuffle);
        }

        public DeckMoveResult DiscardFromHand(
            DeckCommandId commandId,
            CardInstanceId instanceId)
        {
            var before = Counts();
            var validationFailure = Validate(commandId);
            if (validationFailure != DeckOperationReason.None)
            {
                return Failure(validationFailure, 1, before);
            }

            if (!instanceId.IsValid)
            {
                return Failure(DeckOperationReason.InvalidRequest, 1, before);
            }

            var handIndex = FindHandIndex(instanceId);
            if (handIndex < 0)
            {
                return Failure(DeckOperationReason.CardNotInHand, 1, before);
            }

            var card = _hand[handIndex];
            _hand.RemoveAt(handIndex);
            _discardPile.Add(card);
            _completedCommands.Add(commandId);
            return new DeckMoveResult(
                true,
                DeckOperationReason.None,
                1,
                before,
                Counts(),
                new[] { instanceId },
                DeckShuffleInfo.None());
        }

        public DeckMoveResult ForceDiscardHand(DeckCommandId commandId)
        {
            var before = Counts();
            var validationFailure = Validate(commandId);
            if (validationFailure != DeckOperationReason.None)
            {
                return Failure(validationFailure, before.Hand, before);
            }

            var handSnapshot = new List<CardInstance>(_hand);
            var movedIds = new List<CardInstanceId>(handSnapshot.Count);
            for (var index = handSnapshot.Count - 1; index >= 0; index--)
            {
                var card = handSnapshot[index];
                var currentIndex = FindHandIndex(card.InstanceId);
                if (currentIndex < 0)
                {
                    continue;
                }

                _hand.RemoveAt(currentIndex);
                _discardPile.Add(card);
                movedIds.Add(card.InstanceId);
            }

            _completedCommands.Add(commandId);
            return new DeckMoveResult(
                true,
                DeckOperationReason.None,
                handSnapshot.Count,
                before,
                Counts(),
                movedIds,
                DeckShuffleInfo.None());
        }

        private static List<CardInstance> CopyAndValidateCards(
            IReadOnlyList<CardInstance> cards)
        {
            var copy = new List<CardInstance>(cards.Count);
            var instanceIds = new HashSet<CardInstanceId>();
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                if (card == null)
                {
                    throw new ArgumentException("Deck cards cannot be null.", nameof(cards));
                }

                if (!instanceIds.Add(card.InstanceId))
                {
                    throw new ArgumentException(
                        "Card instance identities must be globally unique within the battle deck.",
                        nameof(cards));
                }

                copy.Add(card);
            }

            return copy;
        }

        private DeckShuffleInfo RecycleDiscardIntoDrawPile()
        {
            var movedIds = new List<CardInstanceId>(_discardPile.Count);
            for (var index = 0; index < _discardPile.Count; index++)
            {
                var card = _discardPile[index];
                _drawPile.Add(card);
                movedIds.Add(card.InstanceId);
            }

            _discardPile.Clear();
            _random.Shuffle(_drawPile);
            return new DeckShuffleInfo(true, movedIds);
        }

        private DeckOperationReason Validate(DeckCommandId commandId, int requestedCount)
        {
            if (requestedCount <= 0)
            {
                return DeckOperationReason.InvalidRequest;
            }

            return Validate(commandId);
        }

        private DeckOperationReason Validate(DeckCommandId commandId)
        {
            if (!commandId.IsValid)
            {
                return DeckOperationReason.InvalidCommand;
            }

            return _completedCommands.Contains(commandId)
                ? DeckOperationReason.DuplicateCommand
                : DeckOperationReason.None;
        }

        private DeckMoveResult Failure(
            DeckOperationReason reason,
            int requestedCount,
            DeckZoneCounts before)
        {
            return new DeckMoveResult(
                false,
                reason,
                requestedCount,
                before,
                Counts(),
                Array.Empty<CardInstanceId>(),
                DeckShuffleInfo.None());
        }

        private DeckZoneCounts Counts()
        {
            return new DeckZoneCounts(
                _drawPile.Count,
                _hand.Count,
                _discardPile.Count);
        }

        private int FindHandIndex(CardInstanceId instanceId)
        {
            for (var index = 0; index < _hand.Count; index++)
            {
                if (_hand[index].InstanceId == instanceId)
                {
                    return index;
                }
            }

            return -1;
        }

        private static DeckOperationReason ResolveDrawReason(
            int requestedCount,
            int startingCapacity,
            int movedCount)
        {
            if (movedCount == requestedCount)
            {
                return DeckOperationReason.None;
            }

            if (startingCapacity < requestedCount && movedCount == startingCapacity)
            {
                return DeckOperationReason.PartialHandLimit;
            }

            return DeckOperationReason.PartialExhausted;
        }
    }
}
