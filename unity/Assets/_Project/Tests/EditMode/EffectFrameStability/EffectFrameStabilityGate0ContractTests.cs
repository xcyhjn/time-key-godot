using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.EffectFrameStability
{
    public sealed class EffectFrameStabilityGate0ContractTests
    {
        [Test]
        public void InteractionContract_ProvidesCoordinatorAndTileInspectionPort()
        {
            Assert.That(FindType("TimeKey.Presentation.Interaction.OverlayPriorityCoordinator"), Is.Not.Null);
            Assert.That(FindType("TimeKey.Presentation.Interaction.IdleTileInspectPort"), Is.Not.Null);
        }

        [Test]
        public void IdentityContract_ProvidesActionCardMapLink()
        {
            Assert.That(FindType("TimeKey.Presentation.Identity.ActionIdentityLink"), Is.Not.Null);
        }

        [Test]
        public void TimelineFrameContract_ExposesOnlyRealOccupiedGeometry()
        {
            var frameType = FindType("TimeKey.Presentation.Actions.TimelineActionFrame");
            Assert.That(frameType, Is.Not.Null);
            var property = frameType.GetProperty("VisualOccupiedCells");
            Assert.That(
                property,
                Is.Not.Null,
                "TimelineActionFrame must expose the real occupied-cell geometry contract before Gate B.");
            Assert.That(
                typeof(IEnumerable<TimelineCell>).IsAssignableFrom(property.PropertyType),
                Is.True,
                "VisualOccupiedCells must be a typed TimelineCell collection.");
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(type => type != null);
        }
    }
}
