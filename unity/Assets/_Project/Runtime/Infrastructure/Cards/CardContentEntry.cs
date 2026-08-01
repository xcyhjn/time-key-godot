using System;
using System.IO;
using TimeKey.Domain;

namespace TimeKey.Infrastructure.Cards
{
    public sealed class CardContentEntry
    {
        public CardContentEntry(CardDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            ArtworkResourcePath = "Art/Battle/Cards/" +
                Path.GetFileNameWithoutExtension(definition.FrontImage);
        }

        public CardDefinition Definition { get; }

        public string StableId => Definition.StableId;

        public string FrontImage => Definition.FrontImage;

        public string ArtworkResourcePath { get; }
    }
}
