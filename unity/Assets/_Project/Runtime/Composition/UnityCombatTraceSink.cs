using System;
using System.Text;
using TimeKey.Application;
using UnityEngine;

namespace TimeKey.Composition
{
    [DisallowMultipleComponent]
    public sealed class UnityCombatTraceSink : MonoBehaviour, ICombatTraceSink
    {
        [SerializeField] private bool loggingEnabled = true;

        public bool LoggingEnabled
        {
            get => loggingEnabled;
            set => loggingEnabled = value;
        }

        public void Record(CombatTraceEntry entry)
        {
            if (!loggingEnabled)
            {
                return;
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var message = new StringBuilder("TIMEKEY_COMBAT_TRACE");
            Append(message, "command", entry.Command);
            Append(message, "phaseBefore", entry.PhaseBefore);
            Append(message, "phaseAfter", entry.PhaseAfter);
            Append(message, "card", entry.StableId);
            Append(message, "target", entry.TargetId);
            Append(message, "coordinate", entry.TargetCoordinate?.ToString());
            Append(message, "origin", entry.TimelineOrigin?.ToString());
            Append(message, "effect", entry.EffectKind?.ToString());
            Append(message, "before", entry.BeforeValue?.ToString());
            Append(message, "after", entry.AfterValue?.ToString());
            Append(message, "failure", entry.FailureReason);
            Debug.Log(message.ToString(), this);
        }

        private static void Append(StringBuilder message, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                message.Append('|').Append(key).Append('=').Append(value);
            }
        }
    }
}
