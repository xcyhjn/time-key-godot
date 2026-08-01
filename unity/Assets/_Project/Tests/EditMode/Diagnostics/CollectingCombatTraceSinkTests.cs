using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Diagnostics;

namespace TimeKey.Tests.EditMode.Diagnostics
{
    public sealed class CollectingCombatTraceSinkTests
    {
        [Test]
        public void RecordPreservesInsertionOrder()
        {
            var sink = new CollectingCombatTraceSink();
            var first = new CombatTraceEntry("first", "Idle", "CardSelected");
            var second = new CombatTraceEntry("second", "CardSelected", "TargetSelected");

            sink.Record(first);
            sink.Record(second);

            Assert.That(sink.Entries, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void ClearIsRepeatableAndRecordRejectsNull()
        {
            var sink = new CollectingCombatTraceSink();
            sink.Record(new CombatTraceEntry("first", "Idle", "Idle"));

            sink.Clear();
            sink.Clear();

            Assert.That(sink.Entries, Is.Empty);
            Assert.That(
                () => sink.Record(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("entry"));
        }
    }
}
