using TimeKey.Application;

namespace TimeKey.Diagnostics
{
    public sealed class NoOpCombatTraceSink : ICombatTraceSink
    {
        public static readonly NoOpCombatTraceSink Instance = new NoOpCombatTraceSink();

        private NoOpCombatTraceSink()
        {
        }

        public void Record(CombatTraceEntry entry)
        {
        }
    }
}
