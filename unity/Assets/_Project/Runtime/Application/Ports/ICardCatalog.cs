using System.Collections.Generic;
using TimeKey.Domain;

namespace TimeKey.Application
{
    public interface ICardCatalog
    {
        IReadOnlyList<CardDefinition> Cards { get; }

        bool TryGet(string stableId, out CardDefinition card);
    }
}
