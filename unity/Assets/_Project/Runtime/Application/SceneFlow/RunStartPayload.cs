using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Application.SceneFlow
{
    public enum RunStartKind
    {
        NewGame,
        SeedGame
    }

    public sealed class RunStartPayload : ISceneTransitionPayload
    {
        private readonly ReadOnlyCollection<string> _deckStableIds;

        public RunStartPayload(
            RunStartKind kind,
            string runId,
            int runSeed,
            string seedText,
            int chapter,
            int era,
            int phase,
            int timecoins,
            IReadOnlyList<string> deckStableIds)
            : this(
                kind,
                runId,
                runSeed,
                seedText,
                chapter,
                era,
                phase,
                timecoins,
                "silver-character",
                deckStableIds)
        {
        }

        public RunStartPayload(
            RunStartKind kind,
            string runId,
            int runSeed,
            string seedText,
            int chapter,
            int era,
            int phase,
            int timecoins,
            string characterId,
            IReadOnlyList<string> deckStableIds)
        {
            if (string.IsNullOrWhiteSpace(runId))
            {
                throw new ArgumentException("A run identity is required.", nameof(runId));
            }

            if (chapter <= 0 || era <= 0 || phase <= 0 || timecoins < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chapter),
                    "Run state values must be positive and timecoins cannot be negative.");
            }

            if (deckStableIds == null || deckStableIds.Count == 0)
            {
                throw new ArgumentException("A starter deck is required.", nameof(deckStableIds));
            }

            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException("A character identity is required.", nameof(characterId));
            }

            Kind = kind;
            RunId = runId;
            RunSeed = runSeed;
            SeedText = seedText ?? string.Empty;
            Chapter = chapter;
            Era = era;
            Phase = phase;
            Timecoins = timecoins;
            CharacterId = characterId;
            _deckStableIds = new ReadOnlyCollection<string>(
                new List<string>(deckStableIds));
        }

        public SceneId TargetScene => SceneId.OutOfBattleShell;

        public RunStartKind Kind { get; }

        public string RunId { get; }

        public int RunSeed { get; }

        public string SeedText { get; }

        public int Chapter { get; }

        public int Era { get; }

        public int Phase { get; }

        public int Timecoins { get; }

        public string CharacterId { get; }

        public IReadOnlyList<string> DeckStableIds => _deckStableIds;

        public string Fingerprint =>
            "run-start:" + Kind + ":" + RunId + ":" + RunSeed + ":" + SeedText + ":" +
            Chapter + ":" + Era + ":" + Phase + ":" + Timecoins + ":" +
            CharacterId + ":" +
            string.Join(",", _deckStableIds);
    }
}
