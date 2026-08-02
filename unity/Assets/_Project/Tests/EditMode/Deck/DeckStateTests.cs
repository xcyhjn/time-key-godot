using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain.Deck;

namespace TimeKey.Tests.EditMode.Deck
{
    public sealed class DeckStateTests
    {
        [Test]
        public void StarterDeck_UsesFrozenOrderAndUniqueInstances()
        {
            var expected = new[]
            {
                "lighting", "lighting", "earthquake", "earthquake",
                "recover", "wind", "wind", "recover", "tower", "tower",
                "poison", "poison"
            };
            var cards = StarterDeck.CreateInstances("battle-a");

            Assert.That(StarterDeck.OrderedStableIds, Is.EqualTo(expected));
            Assert.That(cards.Select(card => card.StableId), Is.EqualTo(expected));
            Assert.That(cards.Select(card => card.InstanceId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(cards[0].StableId, Is.EqualTo(cards[1].StableId));
            Assert.That(cards[0].InstanceId, Is.Not.EqualTo(cards[1].InstanceId));
        }

        [Test]
        public void Constructor_RejectsDuplicateInstanceIdentity()
        {
            var duplicateId = new CardInstanceId("duplicate");
            var cards = new[]
            {
                new CardInstance("lighting", duplicateId),
                new CardInstance("wind", duplicateId)
            };

            Assert.Throws<ArgumentException>(() => new DeckState(cards, 1, false));
        }

        [Test]
        public void InitialShuffle_IsRepeatableForSameSeedAndDiffersForDifferentSeed()
        {
            var first = DeckState.CreateStarter(731, "battle-a").Snapshot();
            var repeated = DeckState.CreateStarter(731, "battle-b").Snapshot();
            var different = DeckState.CreateStarter(732, "battle-c").Snapshot();

            Assert.That(
                repeated.DrawPile.Select(card => card.StableId),
                Is.EqualTo(first.DrawPile.Select(card => card.StableId)));
            Assert.That(
                different.DrawPile.Select(card => card.StableId),
                Is.Not.EqualTo(first.DrawPile.Select(card => card.StableId)));
        }

        [Test]
        public void Draw_UsesHighIndexAsTop()
        {
            var state = CreateOrderedState("lighting", "wind", "tower");

            var result = state.Draw(Command("draw-top"), 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.MovedInstanceIds,
                Is.EqualTo(new[] { Id(2), Id(1) }));
            Assert.That(
                state.Snapshot().Hand.Select(card => card.StableId),
                Is.EqualTo(new[] { "tower", "wind" }));
            Assert.That(result.Before, Is.EqualTo(new DeckZoneCounts(3, 0, 0)));
            Assert.That(result.After, Is.EqualTo(new DeckZoneCounts(1, 2, 0)));
        }

        [Test]
        public void Draw_StopsAtHandLimitAndReturnsTypedPartialResult()
        {
            var state = CreateOrderedState(
                "lighting", "wind", "tower", "poison", "recover",
                "earthquake", "lighting", "wind", "tower");
            Assert.That(state.Draw(Command("first-six"), 6).MovedCount, Is.EqualTo(6));

            var result = state.Draw(Command("over-limit"), 3);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Reason, Is.EqualTo(DeckOperationReason.PartialHandLimit));
            Assert.That(result.MovedCount, Is.EqualTo(1));
            Assert.That(result.After.Hand, Is.EqualTo(DeckState.HandLimit));
            Assert.That(state.Snapshot().Counts.Total, Is.EqualTo(9));
        }

        [Test]
        public void Draw_WhenPileEmpty_RecyclesDiscardOnceAndPreservesInstances()
        {
            var state = CreateOrderedState("lighting", "wind", "tower");
            var initialIds = state.Snapshot().DrawPile.Select(card => card.InstanceId).ToArray();
            Assert.That(state.Draw(Command("draw-all"), 3).MovedCount, Is.EqualTo(3));
            Assert.That(state.ForceDiscardHand(Command("discard-all")).MovedCount, Is.EqualTo(3));

            var result = state.Draw(Command("recycle"), 2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Shuffle.Occurred, Is.True);
            Assert.That(result.Shuffle.MovedCardCount, Is.EqualTo(3));
            Assert.That(result.Shuffle.MovedInstanceIds, Is.EquivalentTo(initialIds));
            Assert.That(result.After, Is.EqualTo(new DeckZoneCounts(1, 2, 0)));
        }

        [Test]
        public void Draw_WhenBothPilesEmpty_ReturnsExhaustedWithoutMutation()
        {
            var state = CreateOrderedState();

            var result = state.Draw(Command("empty"), DeckState.FormalDrawRequest);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DeckOperationReason.Exhausted));
            Assert.That(result.Before, Is.EqualTo(result.After));
            Assert.That(result.MovedInstanceIds, Is.Empty);
            Assert.That(result.Shuffle.Occurred, Is.False);
            Assert.That(state.Snapshot().Counts.Total, Is.Zero);
        }

        [Test]
        public void DrawAndDiscard_AreStableAcrossMultipleRecycleCycles()
        {
            var state = CreateOrderedState("lighting", "wind");
            var expectedIds = state.Snapshot().DrawPile.Select(card => card.InstanceId).ToArray();

            for (var cycle = 0; cycle < 3; cycle++)
            {
                var draw = state.Draw(Command("draw-" + cycle), 2);
                var discard = state.ForceDiscardHand(Command("discard-" + cycle));

                Assert.That(draw.MovedInstanceIds, Is.EquivalentTo(expectedIds));
                Assert.That(discard.MovedInstanceIds, Is.EquivalentTo(expectedIds));
                Assert.That(state.Snapshot().Counts, Is.EqualTo(new DeckZoneCounts(0, 0, 2)));
            }
        }

        [Test]
        public void ForceDiscardHand_EmptyHandIsSuccessfulNoOpAndCommandIsIdempotent()
        {
            var state = CreateOrderedState("lighting");
            var command = Command("empty-discard");

            var first = state.ForceDiscardHand(command);
            var duplicate = state.ForceDiscardHand(command);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Reason, Is.EqualTo(DeckOperationReason.None));
            Assert.That(first.Before, Is.EqualTo(first.After));
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.Reason, Is.EqualTo(DeckOperationReason.DuplicateCommand));
            Assert.That(duplicate.Before, Is.EqualTo(duplicate.After));
        }

        [Test]
        public void InvalidAndDuplicateDraw_DoNotChangeState()
        {
            var state = CreateOrderedState("lighting", "wind");
            var invalid = state.Draw(Command("invalid"), 0);
            var command = Command("draw-once");
            var first = state.Draw(command, 1);
            var duplicate = state.Draw(command, 1);

            Assert.That(invalid.Succeeded, Is.False);
            Assert.That(invalid.Reason, Is.EqualTo(DeckOperationReason.InvalidRequest));
            Assert.That(invalid.Before, Is.EqualTo(invalid.After));
            Assert.That(first.Succeeded, Is.True);
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.Reason, Is.EqualTo(DeckOperationReason.DuplicateCommand));
            Assert.That(duplicate.Before, Is.EqualTo(duplicate.After));
            Assert.That(state.Snapshot().Counts, Is.EqualTo(first.After));
        }

        [Test]
        public void DiscardFromHand_MovesOnlyRequestedInstanceAndMissingCardDoesNothing()
        {
            var state = CreateOrderedState("lighting", "wind");
            var draw = state.Draw(Command("draw-two"), 2);
            var selected = draw.MovedInstanceIds[0];

            var discarded = state.DiscardFromHand(Command("play-card"), selected);
            var missing = state.DiscardFromHand(Command("missing-card"), Id(99));

            Assert.That(discarded.Succeeded, Is.True);
            Assert.That(discarded.MovedInstanceIds, Is.EqualTo(new[] { selected }));
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.Reason, Is.EqualTo(DeckOperationReason.CardNotInHand));
            Assert.That(missing.Before, Is.EqualTo(missing.After));
            Assert.That(state.Snapshot().Counts, Is.EqualTo(new DeckZoneCounts(0, 1, 1)));
        }

        [Test]
        public void SnapshotsAndResults_AreDefensiveCopies()
        {
            var source = new List<CardInstance>
            {
                new CardInstance("lighting", Id(0)),
                new CardInstance("wind", Id(1))
            };
            var state = new DeckState(source, 731, false);
            var before = state.Snapshot();
            source.Clear();
            var result = state.Draw(Command("draw"), 1);

            Assert.That(before.DrawPile.Count, Is.EqualTo(2));
            Assert.That(state.Snapshot().Counts.Total, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CardInstance>)before.DrawPile).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CardInstanceId>)result.MovedInstanceIds).Clear());
            Assert.That(result.MovedCount, Is.EqualTo(1));
        }

        private static DeckState CreateOrderedState(params string[] stableIds)
        {
            var cards = stableIds
                .Select((stableId, index) => new CardInstance(stableId, Id(index)))
                .ToArray();
            return new DeckState(cards, 731, false);
        }

        private static CardInstanceId Id(int ordinal)
        {
            return CardInstanceId.FromOrdinal("test", ordinal);
        }

        private static DeckCommandId Command(string value)
        {
            return new DeckCommandId(value);
        }
    }
}
