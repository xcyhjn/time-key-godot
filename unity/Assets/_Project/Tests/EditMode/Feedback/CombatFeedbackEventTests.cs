using System.Threading;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation.Feedback;
using UnityEngine;

namespace TimeKey.Tests.EditMode.Feedback
{
    public sealed class CombatFeedbackEventTests
    {
        [Test]
        public void Event_CopiesValueAnchorAndExposesCancellationWithoutUnityObjectOwnership()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var anchor = new CombatFeedbackWorldAnchor(
                    new HexCoord(2, -1),
                    new Vector3(4f, 1.64f, -2f),
                    "enemy-01/status");
                var value = new CombatFeedbackEvent(
                    17,
                    "lighting",
                    "enemy-01",
                    anchor,
                    CombatFeedbackKind.Damage,
                    CombatFeedbackPhase.Playing,
                    0.35f,
                    cancellation.Token);

                Assert.That(value.Sequence, Is.EqualTo(17));
                Assert.That(value.SourceId, Is.EqualTo("lighting"));
                Assert.That(value.TargetId, Is.EqualTo("enemy-01"));
                Assert.That(value.WorldAnchor.Coordinate, Is.EqualTo(new HexCoord(2, -1)));
                Assert.That(value.WorldAnchor.WorldPosition, Is.EqualTo(new Vector3(4f, 1.64f, -2f)));
                Assert.That(value.WorldAnchor.AnchorId, Is.EqualTo("enemy-01/status"));
                Assert.That(value.IsCancellable, Is.True);
                Assert.That(value.IsNoEffect, Is.False);
            }
        }

        [Test]
        public void Cancel_ReturnsNewTerminalValueWithoutMutatingOriginal()
        {
            var value = new CombatFeedbackEvent(
                3,
                "enemy-intent",
                "player",
                CombatFeedbackWorldAnchor.FromCoordinate(new HexCoord(0, 0), "player"),
                CombatFeedbackKind.UnsupportedSourceCommand,
                CombatFeedbackPhase.Completed,
                0f);

            var cancelled = value.Cancel();

            Assert.That(value.Phase, Is.EqualTo(CombatFeedbackPhase.Completed));
            Assert.That(cancelled, Is.Not.SameAs(value));
            Assert.That(cancelled.Sequence, Is.EqualTo(value.Sequence));
            Assert.That(cancelled.Phase, Is.EqualTo(CombatFeedbackPhase.Cancelled));
            Assert.That(cancelled.Duration, Is.Zero);
            Assert.That(cancelled.IsNoEffect, Is.True);
        }
    }
}
