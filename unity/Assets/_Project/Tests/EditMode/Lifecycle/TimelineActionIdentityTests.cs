using System;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle
{
    public sealed class TimelineActionIdentityTests
    {
        [Test]
        public void Constructor_RejectsMissingIdentity()
        {
            Assert.Throws<ArgumentException>(() => new TimelineActionIdentity(null));
            Assert.Throws<ArgumentException>(() => new TimelineActionIdentity(string.Empty));
            Assert.Throws<ArgumentException>(() => new TimelineActionIdentity("   "));
        }

        [Test]
        public void Equality_IsOrdinalValueEquality()
        {
            var first = new TimelineActionIdentity("cycle:1/action:0");
            var equal = new TimelineActionIdentity("cycle:1/action:0");
            var differentCase = new TimelineActionIdentity("Cycle:1/action:0");

            Assert.That(equal, Is.EqualTo(first));
            Assert.That(equal.GetHashCode(), Is.EqualTo(first.GetHashCode()));
            Assert.That(differentCase, Is.Not.EqualTo(first));
            Assert.That(first == equal, Is.True);
            Assert.That(first != differentCase, Is.True);
        }

        [Test]
        public void FromSequence_UsesCycleAndDeterministicOrdinal()
        {
            var first = TimelineActionIdentity.FromSequence(4, 0);
            var repeated = TimelineActionIdentity.FromSequence(4, 0);
            var next = TimelineActionIdentity.FromSequence(4, 1);

            Assert.That(first.Value, Is.EqualTo("cycle:4/action:0"));
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(next, Is.Not.EqualTo(first));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TimelineActionIdentity.FromSequence(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TimelineActionIdentity.FromSequence(1, -1));
        }
    }
}
