using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public sealed class OccupantEffectResult
    {
        public OccupantEffectResult(
            CardEffectKind effectKind,
            CombatOccupantSnapshot before,
            CombatOccupantSnapshot after)
        {
            if (!Enum.IsDefined(typeof(CardEffectKind), effectKind))
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            if (before == null && after == null)
            {
                throw new ArgumentException("An occupant effect result requires a before or after snapshot.");
            }

            EffectKind = effectKind;
            Before = before;
            After = after;
        }

        public CardEffectKind EffectKind { get; }

        public CombatOccupantSnapshot Before { get; }

        public CombatOccupantSnapshot After { get; }
    }

    public sealed class CardEffectResultBuffer
    {
        private readonly List<TileEffectResult> _tileResults = new List<TileEffectResult>();
        private readonly List<OccupantEffectResult> _occupantResults =
            new List<OccupantEffectResult>();

        public IReadOnlyList<TileEffectResult> TileResults =>
            new ReadOnlyCollection<TileEffectResult>(_tileResults);

        public IReadOnlyList<OccupantEffectResult> OccupantResults =>
            new ReadOnlyCollection<OccupantEffectResult>(_occupantResults);

        public void AddTile(TileEffectResult result)
        {
            _tileResults.Add(result ?? throw new ArgumentNullException(nameof(result)));
        }

        public void AddOccupant(OccupantEffectResult result)
        {
            _occupantResults.Add(result ?? throw new ArgumentNullException(nameof(result)));
        }
    }
}
