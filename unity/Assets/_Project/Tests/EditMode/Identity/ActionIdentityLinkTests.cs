using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Domain.Deck;
using TimeKey.Presentation.Identity;

namespace TimeKey.Tests.EditMode.Identity
{
    public sealed class ActionIdentityLinkTests
    {
        [Test]
        public void DuplicateStableIdsRemainDistinctAndInstanceLookupIsExact()
        {
            var index = new ActionIdentityIndex();
            var first = new TimelineActionIdentity("cycle:1/action:0");
            var second = new TimelineActionIdentity("cycle:1/action:1");
            var firstInstance = CardInstanceId.FromOrdinal("run", 0);
            var secondInstance = CardInstanceId.FromOrdinal("run", 1);
            index.Add(new ActionIdentityLink(first, "lighting", firstInstance, "enemy-a", "target-a"));
            index.Add(new ActionIdentityLink(second, "lighting", secondInstance, "enemy-b", "target-b"));

            Assert.That(index.FindByStableId("lighting"), Has.Count.EqualTo(2));
            Assert.That(index.TryGetAction(secondInstance, out var link), Is.True);
            Assert.That(link.ActionId, Is.EqualTo(second));
            Assert.That(index.FindByMapRuntimeId("target-b"), Is.EquivalentTo(new[] { second }));
            Assert.That(index.Remove(first), Is.True);
            Assert.That(index.FindByStableId("lighting"), Is.EquivalentTo(new[] { second }));
        }

        [Test]
        public void RemovingActionClearsAllReverseIndexesIdempotently()
        {
            var index = new ActionIdentityIndex();
            var action = new TimelineActionIdentity("cycle:2/action:0");
            var instance = CardInstanceId.FromOrdinal("run", 4);
            index.Add(new ActionIdentityLink(action, "poison", instance, "source", "target"));
            Assert.That(index.Remove(action), Is.True);
            Assert.That(index.Remove(action), Is.False);
            Assert.That(index.FindByStableId("poison"), Is.Empty);
            Assert.That(index.FindByMapRuntimeId("source"), Is.Empty);
            Assert.That(index.TryGetAction(instance, out _), Is.False);
        }
    }
}
