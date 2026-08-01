using System.Collections.Generic;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Application
{
    public interface IEnemyIntentSourceCatalog
    {
        IReadOnlyList<EnemyIntentSourceSnapshot> CaptureSources(CombatSliceState state);
    }
}
