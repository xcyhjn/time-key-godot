using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Application.SceneFlow
{
    public sealed class CombatLaunchPayload : ISceneTransitionPayload
    {
        private readonly ReadOnlyCollection<string> _deckStableIds;

        public CombatLaunchPayload(
            string launchCorrelationId,
            string runId,
            int runSeed,
            int chapter,
            int era,
            int phase,
            string roomId,
            string characterId,
            int timecoins,
            string battleTag,
            int battleSeed,
            IReadOnlyList<string> deckStableIds)
        {
            LaunchCorrelationId = Required(launchCorrelationId, nameof(launchCorrelationId));
            RunId = Required(runId, nameof(runId));
            RoomId = Required(roomId, nameof(roomId));
            CharacterId = Required(characterId, nameof(characterId));
            BattleTag = Required(battleTag, nameof(battleTag));
            if (chapter <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chapter));
            }

            if (era <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(era));
            }

            if (phase < 1 || phase > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (timecoins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timecoins));
            }

            RunSeed = runSeed;
            Chapter = chapter;
            Era = era;
            Phase = phase;
            Timecoins = timecoins;
            BattleSeed = battleSeed;
            _deckStableIds = CopyDeck(deckStableIds);
        }

        public SceneId TargetScene => SceneId.Combat;

        public string LaunchCorrelationId { get; }

        public string RunId { get; }

        public int RunSeed { get; }

        public int Chapter { get; }

        public int Era { get; }

        public int Phase { get; }

        public string RoomId { get; }

        public string CharacterId { get; }

        public int Timecoins { get; }

        public string BattleTag { get; }

        public int BattleSeed { get; }

        public IReadOnlyList<string> DeckStableIds => _deckStableIds;

        public string Fingerprint =>
            "combat:" + LaunchCorrelationId + ":" + RunId + ":" + RoomId + ":" +
            CharacterId + ":" + RunSeed + ":" + Chapter + ":" + BattleSeed + ":" +
            Era + ":" + Phase + ":" + Timecoins + ":" +
            string.Join(",", _deckStableIds);

        private static ReadOnlyCollection<string> CopyDeck(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                throw new ArgumentException("A non-empty deck snapshot is required.", nameof(source));
            }

            var copied = new List<string>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                copied.Add(Required(source[index], nameof(source)));
            }

            return new ReadOnlyCollection<string>(copied);
        }

        private static string Required(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }
    }
}
