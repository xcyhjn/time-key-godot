using System;
using TimeKey.Domain;

namespace TimeKey.Infrastructure.Effects
{
    public sealed class CardEffectRegistration
    {
        public CardEffectRegistration(CardEffectKind kind)
        {
            if (!Enum.IsDefined(typeof(CardEffectKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            Kind = kind;
        }

        public CardEffectKind Kind { get; }
    }
}
