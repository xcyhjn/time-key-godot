using System;
using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Application.Lifecycle
{
    public sealed class TimelineActionPresentationSnapshotTests
    {
        [Test]
        public void Constructor_DeepCopiesEveryCollection()
        {
            var shape = new List<TimelineCell>
            {
                new TimelineCell(0, 0),
                new TimelineCell(1, 0)
            };
            var occupiedCells = new List<TimelineCell>
            {
                new TimelineCell(4, 1),
                new TimelineCell(5, 1)
            };
            var effectRange = new List<HexCoord>
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            };

            var snapshot = Create(
                TimelineActionValidity.Valid,
                TimelineActionInvalidReason.None,
                TimelineActionResolveState.Scheduled,
                shape,
                occupiedCells,
                effectRange);

            shape[0] = new TimelineCell(9, 2);
            occupiedCells.Clear();
            effectRange.Add(new HexCoord(9, 9));

            Assert.That(snapshot.Shape, Is.EqualTo(new[]
            {
                new TimelineCell(0, 0),
                new TimelineCell(1, 0)
            }));
            Assert.That(snapshot.OccupiedCells, Is.EqualTo(new[]
            {
                new TimelineCell(4, 1),
                new TimelineCell(5, 1)
            }));
            Assert.That(snapshot.EffectRange, Is.EqualTo(new[]
            {
                new HexCoord(0, 0),
                new HexCoord(1, 0)
            }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<TimelineCell>)snapshot.Shape).Add(new TimelineCell(2, 0)));
        }

        [TestCase(
            TimelineActionValidity.Valid,
            TimelineActionInvalidReason.None,
            TimelineActionResolveState.Resolving)]
        [TestCase(
            TimelineActionValidity.Valid,
            TimelineActionInvalidReason.None,
            TimelineActionResolveState.Resolved)]
        [TestCase(
            TimelineActionValidity.Invalid,
            TimelineActionInvalidReason.TargetUnavailable,
            TimelineActionResolveState.Removed)]
        [TestCase(
            TimelineActionValidity.Unsupported,
            TimelineActionInvalidReason.UnsupportedSourceCommand,
            TimelineActionResolveState.Resolved)]
        public void TypedValidityReasonAndResolveState_ArePreserved(
            TimelineActionValidity validity,
            TimelineActionInvalidReason reason,
            TimelineActionResolveState resolveState)
        {
            var snapshot = Create(validity, reason, resolveState);

            Assert.That(snapshot.Validity, Is.EqualTo(validity));
            Assert.That(snapshot.InvalidReason, Is.EqualTo(reason));
            Assert.That(snapshot.ResolveState, Is.EqualTo(resolveState));
            Assert.That(snapshot.ActionId, Is.EqualTo(new TimelineActionIdentity("action-1")));
            Assert.That(snapshot.ActorKind, Is.EqualTo(TimelineActorKind.Enemy));
            Assert.That(snapshot.SourceId, Is.EqualTo("source-1"));
            Assert.That(snapshot.SourceCoord, Is.EqualTo(new HexCoord(1, 2)));
            Assert.That(snapshot.TargetId, Is.EqualTo("target-1"));
            Assert.That(snapshot.TargetCoord, Is.EqualTo(new HexCoord(2, 2)));
            Assert.That(snapshot.EffectStableId, Is.EqualTo("enemy-intent"));
            Assert.That(snapshot.Display.Title, Is.EqualTo("Intent"));
        }

        [Test]
        public void Constructor_RejectsValidityAndReasonMismatch()
        {
            Assert.Throws<ArgumentException>(() => Create(
                TimelineActionValidity.Valid,
                TimelineActionInvalidReason.OutOfBounds,
                TimelineActionResolveState.Preview));
            Assert.Throws<ArgumentException>(() => Create(
                TimelineActionValidity.Invalid,
                TimelineActionInvalidReason.None,
                TimelineActionResolveState.Removed));
            Assert.Throws<ArgumentException>(() => Create(
                TimelineActionValidity.Unsupported,
                TimelineActionInvalidReason.TargetUnavailable,
                TimelineActionResolveState.Resolved));
        }

        private static TimelineActionPresentationSnapshot Create(
            TimelineActionValidity validity,
            TimelineActionInvalidReason reason,
            TimelineActionResolveState resolveState,
            IReadOnlyList<TimelineCell> shape = null,
            IReadOnlyList<TimelineCell> occupiedCells = null,
            IReadOnlyList<HexCoord> effectRange = null)
        {
            return new TimelineActionPresentationSnapshot(
                new TimelineActionIdentity("action-1"),
                TimelineActorKind.Enemy,
                999,
                "source-1",
                new HexCoord(1, 2),
                "target-1",
                new HexCoord(2, 2),
                null,
                "enemy-intent",
                new TimelineActionDisplayPayload(
                    "Intent",
                    "Unsupported source command",
                    "intent-icon",
                    "Source",
                    "Target"),
                new TimelineCell(4, 1),
                shape ?? new[] { new TimelineCell(0, 0) },
                occupiedCells ?? new[] { new TimelineCell(4, 1) },
                effectRange ?? new[] { new HexCoord(0, 0) },
                validity,
                reason,
                resolveState);
        }
    }
}
