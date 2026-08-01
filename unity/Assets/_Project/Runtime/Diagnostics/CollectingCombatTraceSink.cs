using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Application;

namespace TimeKey.Diagnostics
{
    public sealed class CollectingCombatTraceSink : ICombatTraceSink
    {
        private readonly List<CombatTraceEntry> _entries = new List<CombatTraceEntry>();
        private readonly ReadOnlyCollection<CombatTraceEntry> _readOnlyEntries;

        public CollectingCombatTraceSink()
        {
            _readOnlyEntries = _entries.AsReadOnly();
        }

        public IReadOnlyList<CombatTraceEntry> Entries => _readOnlyEntries;

        public void Record(CombatTraceEntry entry)
        {
            _entries.Add(entry ?? throw new ArgumentNullException(nameof(entry)));
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
