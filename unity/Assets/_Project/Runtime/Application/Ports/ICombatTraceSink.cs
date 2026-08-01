using System;
using TimeKey.Domain;

namespace TimeKey.Application
{
    public interface ICombatTraceSink
    {
        void Record(CombatTraceEntry entry);
    }

    public sealed class CombatTraceEntry
    {
        public CombatTraceEntry(
            string command,
            string phaseBefore,
            string phaseAfter,
            string stableId = null,
            string targetId = null,
            HexCoord? targetCoordinate = null,
            TimelineCell? timelineOrigin = null,
            string failureReason = null)
        {
            Command = Require(command, nameof(command));
            PhaseBefore = Require(phaseBefore, nameof(phaseBefore));
            PhaseAfter = Require(phaseAfter, nameof(phaseAfter));
            StableId = stableId;
            TargetId = targetId;
            TargetCoordinate = targetCoordinate;
            TimelineOrigin = timelineOrigin;
            FailureReason = failureReason;
        }

        public string Command { get; }

        public string PhaseBefore { get; }

        public string PhaseAfter { get; }

        public string StableId { get; }

        public string TargetId { get; }

        public HexCoord? TargetCoordinate { get; }

        public TimelineCell? TimelineOrigin { get; }

        public string FailureReason { get; }

        private static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A trace value is required.", parameterName);
            }

            return value;
        }
    }
}
