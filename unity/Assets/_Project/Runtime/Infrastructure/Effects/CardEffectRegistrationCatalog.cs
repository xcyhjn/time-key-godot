using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain;

namespace TimeKey.Infrastructure.Effects
{
    public sealed class CardEffectRegistrationCatalog
    {
        private readonly Dictionary<CardEffectKind, CardEffectRegistration> _registrations;
        private readonly ReadOnlyCollection<CardEffectKind> _supportedKinds;

        public CardEffectRegistrationCatalog(IReadOnlyList<CardEffectRegistration> registrations)
        {
            if (registrations == null)
            {
                throw new ArgumentNullException(nameof(registrations));
            }

            _registrations = new Dictionary<CardEffectKind, CardEffectRegistration>();
            var supportedKinds = new List<CardEffectKind>(registrations.Count);
            for (var index = 0; index < registrations.Count; index++)
            {
                var registration = registrations[index] ??
                    throw new ArgumentException("Effect registrations cannot contain null.", nameof(registrations));
                if (!_registrations.TryAdd(registration.Kind, registration))
                {
                    throw new ArgumentException(
                        "Duplicate effect registration: " + registration.Kind + ".",
                        nameof(registrations));
                }

                supportedKinds.Add(registration.Kind);
            }

            _supportedKinds = new ReadOnlyCollection<CardEffectKind>(supportedKinds);
        }

        public IReadOnlyList<CardEffectKind> SupportedKinds => _supportedKinds;

        public static CardEffectRegistrationCatalog CreateVerticalSlice()
        {
            return new CardEffectRegistrationCatalog(
                new[]
                {
                    new CardEffectRegistration(CardEffectKind.Damage),
                    new CardEffectRegistration(CardEffectKind.Elevation),
                    new CardEffectRegistration(CardEffectKind.Recover),
                    new CardEffectRegistration(CardEffectKind.Built),
                    new CardEffectRegistration(CardEffectKind.Poison)
                });
        }

        public bool Supports(CardEffectKind kind)
        {
            return _registrations.ContainsKey(kind);
        }

        public bool Supports(CardDefinition card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            for (var index = 0; index < card.Effects.Count; index++)
            {
                if (!Supports(card.Effects[index].Kind))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
