using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace TimeKey.Infrastructure.Persistence
{
    public static class OverworldSaveSchema
    {
        public const int CurrentVersion = 2;
        public const int V0DefaultMapConfigVersion = 1;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class OverworldProcessedOutcomeDocument
    {
        public OverworldProcessedOutcomeDocument(
            string outcomeKind,
            string outcomeCorrelationId,
            string fingerprint)
            : this(
                outcomeKind,
                outcomeCorrelationId,
                fingerprint,
                string.Empty,
                new int[0],
                chapterAdvanceKind: 0,
                completedChapter: 0,
                nextChapter: 0)
        {
        }

        [JsonConstructor]
        public OverworldProcessedOutcomeDocument(
            string outcomeKind,
            string outcomeCorrelationId,
            string fingerprint,
            string roomId,
            IEnumerable<int> steps,
            int chapterAdvanceKind,
            int completedChapter,
            int nextChapter)
        {
            OutcomeKind = RequireId(outcomeKind, nameof(outcomeKind));
            OutcomeCorrelationId = RequireId(
                outcomeCorrelationId,
                nameof(outcomeCorrelationId));
            Fingerprint = RequireId(fingerprint, nameof(fingerprint));
            RoomId = OptionalId(roomId);
            Steps = new ReadOnlyCollection<int>(new List<int>(steps ?? new int[0]));
            ChapterAdvanceKind = chapterAdvanceKind;
            CompletedChapter = completedChapter;
            NextChapter = nextChapter;
        }

        [JsonProperty("outcomeKind", Order = 1)]
        public string OutcomeKind { get; }

        [JsonProperty("outcomeCorrelationId", Order = 2)]
        public string OutcomeCorrelationId { get; }

        [JsonProperty("fingerprint", Order = 3)]
        public string Fingerprint { get; }

        [JsonProperty("roomId", Order = 4)]
        public string RoomId { get; }

        [JsonProperty("steps", Order = 5)]
        public IReadOnlyList<int> Steps { get; }

        [JsonProperty("chapterAdvanceKind", Order = 6)]
        public int ChapterAdvanceKind { get; }

        [JsonProperty("completedChapter", Order = 7)]
        public int CompletedChapter { get; }

        [JsonProperty("nextChapter", Order = 8)]
        public int NextChapter { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable identity is required.", parameterName);
            }

            return value.Trim();
        }

        private static string OptionalId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class OverworldOperationJournalDocument
    {
        [JsonConstructor]
        public OverworldOperationJournalDocument(
            long sequence,
            int kind,
            int expectedRevision,
            string sourceNodeId,
            string nodeId,
            int resolution,
            int failure,
            int beforeRevision,
            int afterRevision,
            bool chapterAdvanced)
        {
            if (sequence <= 0 || expectedRevision < 0 || beforeRevision < 0 ||
                afterRevision < beforeRevision)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException("A stable node identity is required.", nameof(nodeId));
            }

            Sequence = sequence;
            Kind = kind;
            ExpectedRevision = expectedRevision;
            SourceNodeId = string.IsNullOrWhiteSpace(sourceNodeId)
                ? string.Empty
                : sourceNodeId.Trim();
            NodeId = nodeId.Trim();
            Resolution = resolution;
            Failure = failure;
            BeforeRevision = beforeRevision;
            AfterRevision = afterRevision;
            ChapterAdvanced = chapterAdvanced;
        }

        [JsonProperty("sequence", Order = 1)] public long Sequence { get; }
        [JsonProperty("kind", Order = 2)] public int Kind { get; }
        [JsonProperty("expectedRevision", Order = 3)] public int ExpectedRevision { get; }
        [JsonProperty("sourceNodeId", Order = 4)] public string SourceNodeId { get; }
        [JsonProperty("nodeId", Order = 5)] public string NodeId { get; }
        [JsonProperty("resolution", Order = 6)] public int Resolution { get; }
        [JsonProperty("failure", Order = 7)] public int Failure { get; }
        [JsonProperty("beforeRevision", Order = 8)] public int BeforeRevision { get; }
        [JsonProperty("afterRevision", Order = 9)] public int AfterRevision { get; }
        [JsonProperty("chapterAdvanced", Order = 10)] public bool ChapterAdvanced { get; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class OverworldSaveDocument
    {
        private readonly ReadOnlyCollection<string> _deckStableIds;
        private readonly ReadOnlyCollection<string> _visitedNodeIds;
        private readonly ReadOnlyCollection<string> _settledNodeIds;
        private readonly ReadOnlyCollection<OverworldProcessedOutcomeDocument> _processedOutcomes;
        private readonly ReadOnlyCollection<OverworldOperationJournalDocument> _domainJournal;

        [JsonConstructor]
        public OverworldSaveDocument(
            int schemaVersion,
            int runStartKind,
            string runId,
            int runSeed,
            string seedText,
            int chapter,
            int finalChapter,
            string mapFingerprint,
            int mapConfigVersion,
            int intermediateLayerCount,
            int minimumNodesPerLayer,
            int maximumNodesPerLayer,
            string currentNodeId,
            string activeRoomId,
            string activeLaunchCorrelationId,
            bool chapterCompleted,
            int chapterAdvanceCount,
            int domainRevision,
            long operationSequenceCursor,
            int persistenceRevision,
            string characterId,
            int era,
            int phase,
            int timecoins,
            IEnumerable<string> deckStableIds,
            IEnumerable<string> visitedNodeIds,
            IEnumerable<string> settledNodeIds,
            IEnumerable<OverworldProcessedOutcomeDocument> processedOutcomes,
            IEnumerable<OverworldOperationJournalDocument> domainJournal = null)
        {
            if (schemaVersion != OverworldSaveSchema.CurrentVersion)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(schemaVersion),
                    "Only the current save schema can construct a save document.");
            }

            if (runStartKind < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(runStartKind));
            }

            if (chapter < 1 || finalChapter < chapter)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chapter),
                    "Chapter values must identify a valid current/final range.");
            }

            if (intermediateLayerCount < 1 || intermediateLayerCount > 32 ||
                minimumNodesPerLayer < 1 ||
                maximumNodesPerLayer < minimumNodesPerLayer ||
                maximumNodesPerLayer > 16)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intermediateLayerCount),
                    "Map generation values are outside the Gate A contract.");
            }

            if (domainRevision < 0 || operationSequenceCursor < 0 ||
                persistenceRevision < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(domainRevision),
                    "Revision and sequence cursors cannot be negative.");
            }

            if (era < 1 || phase < 1 || timecoins < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(era),
                    "Continue resources must preserve positive era/phase and non-negative timecoins.");
            }

            if (chapterAdvanceCount < 0 || chapterAdvanceCount > 1 ||
                chapterCompleted != (chapterAdvanceCount == 1))
            {
                throw new ArgumentException(
                    "Chapter completion and advance count must agree.",
                    nameof(chapterAdvanceCount));
            }

            SchemaVersion = schemaVersion;
            RunStartKind = runStartKind;
            RunId = RequireId(runId, nameof(runId));
            RunSeed = runSeed;
            SeedText = seedText ?? string.Empty;
            Chapter = chapter;
            FinalChapter = finalChapter;
            MapFingerprint = RequireId(mapFingerprint, nameof(mapFingerprint));
            if (mapConfigVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(mapConfigVersion));
            }

            MapConfigVersion = mapConfigVersion;
            IntermediateLayerCount = intermediateLayerCount;
            MinimumNodesPerLayer = minimumNodesPerLayer;
            MaximumNodesPerLayer = maximumNodesPerLayer;
            CurrentNodeId = RequireId(currentNodeId, nameof(currentNodeId));
            ActiveRoomId = OptionalId(activeRoomId);
            ActiveLaunchCorrelationId = OptionalId(activeLaunchCorrelationId);
            ChapterCompleted = chapterCompleted;
            ChapterAdvanceCount = chapterAdvanceCount;
            DomainRevision = domainRevision;
            OperationSequenceCursor = operationSequenceCursor;
            PersistenceRevision = persistenceRevision;
            CharacterId = RequireId(characterId, nameof(characterId));
            Era = era;
            Phase = phase;
            Timecoins = timecoins;
            _deckStableIds = CopyOrderedIds(deckStableIds, nameof(deckStableIds), false);
            _visitedNodeIds = CopyOrderedIds(visitedNodeIds, nameof(visitedNodeIds), true);
            _settledNodeIds = CopyOrderedIds(settledNodeIds, nameof(settledNodeIds), true);
            _processedOutcomes = CopyOutcomes(processedOutcomes);
            _domainJournal = CopyJournal(domainJournal);

            ValidateStateRelationships();
        }

        [JsonProperty("schemaVersion", Order = 1)]
        public int SchemaVersion { get; }

        [JsonProperty("runStartKind", Order = 2)]
        public int RunStartKind { get; }

        [JsonProperty("runId", Order = 3)]
        public string RunId { get; }

        [JsonProperty("runSeed", Order = 4)]
        public int RunSeed { get; }

        [JsonProperty("seedText", Order = 5)]
        public string SeedText { get; }

        [JsonProperty("chapter", Order = 6)]
        public int Chapter { get; }

        [JsonProperty("finalChapter", Order = 7)]
        public int FinalChapter { get; }

        [JsonProperty("mapFingerprint", Order = 8)]
        public string MapFingerprint { get; }

        [JsonProperty("mapConfigVersion", Order = 9)]
        public int MapConfigVersion { get; }

        [JsonProperty("intermediateLayerCount", Order = 10)]
        public int IntermediateLayerCount { get; }

        [JsonProperty("minimumNodesPerLayer", Order = 11)]
        public int MinimumNodesPerLayer { get; }

        [JsonProperty("maximumNodesPerLayer", Order = 12)]
        public int MaximumNodesPerLayer { get; }

        [JsonProperty("currentNodeId", Order = 13)]
        public string CurrentNodeId { get; }

        [JsonProperty("activeRoomId", Order = 14)]
        public string ActiveRoomId { get; }

        [JsonProperty("activeLaunchCorrelationId", Order = 15)]
        public string ActiveLaunchCorrelationId { get; }

        [JsonProperty("chapterCompleted", Order = 16)]
        public bool ChapterCompleted { get; }

        [JsonProperty("chapterAdvanceCount", Order = 17)]
        public int ChapterAdvanceCount { get; }

        [JsonProperty("domainRevision", Order = 18)]
        public int DomainRevision { get; }

        [JsonProperty("operationSequenceCursor", Order = 19)]
        public long OperationSequenceCursor { get; }

        [JsonProperty("persistenceRevision", Order = 20)]
        public int PersistenceRevision { get; }

        [JsonProperty("characterId", Order = 21)]
        public string CharacterId { get; }

        [JsonProperty("era", Order = 22)]
        public int Era { get; }

        [JsonProperty("phase", Order = 23)]
        public int Phase { get; }

        [JsonProperty("timecoins", Order = 24)]
        public int Timecoins { get; }

        [JsonProperty("deckStableIds", Order = 25)]
        public IReadOnlyList<string> DeckStableIds => _deckStableIds;

        [JsonProperty("visitedNodeIds", Order = 26)]
        public IReadOnlyList<string> VisitedNodeIds => _visitedNodeIds;

        [JsonProperty("settledNodeIds", Order = 27)]
        public IReadOnlyList<string> SettledNodeIds => _settledNodeIds;

        [JsonProperty("processedOutcomes", Order = 28)]
        public IReadOnlyList<OverworldProcessedOutcomeDocument> ProcessedOutcomes =>
            _processedOutcomes;

        [JsonProperty("domainJournal", Order = 29)]
        public IReadOnlyList<OverworldOperationJournalDocument> DomainJournal =>
            _domainJournal;

        [JsonIgnore]
        public bool HasActiveRoom => !string.IsNullOrEmpty(ActiveRoomId);

        internal bool ContentEquals(OverworldSaveDocument other)
        {
            if (other == null ||
                SchemaVersion != other.SchemaVersion ||
                RunStartKind != other.RunStartKind ||
                !Same(RunId, other.RunId) ||
                RunSeed != other.RunSeed ||
                !Same(SeedText, other.SeedText) ||
                Chapter != other.Chapter ||
                FinalChapter != other.FinalChapter ||
                !Same(MapFingerprint, other.MapFingerprint) ||
                MapConfigVersion != other.MapConfigVersion ||
                IntermediateLayerCount != other.IntermediateLayerCount ||
                MinimumNodesPerLayer != other.MinimumNodesPerLayer ||
                MaximumNodesPerLayer != other.MaximumNodesPerLayer ||
                !Same(CurrentNodeId, other.CurrentNodeId) ||
                !Same(ActiveRoomId, other.ActiveRoomId) ||
                !Same(ActiveLaunchCorrelationId, other.ActiveLaunchCorrelationId) ||
                ChapterCompleted != other.ChapterCompleted ||
                ChapterAdvanceCount != other.ChapterAdvanceCount ||
                DomainRevision != other.DomainRevision ||
                OperationSequenceCursor != other.OperationSequenceCursor ||
                PersistenceRevision != other.PersistenceRevision ||
                !Same(CharacterId, other.CharacterId) ||
                Era != other.Era || Phase != other.Phase || Timecoins != other.Timecoins ||
                !SameList(_deckStableIds, other._deckStableIds) ||
                !SameList(_visitedNodeIds, other._visitedNodeIds) ||
                !SameList(_settledNodeIds, other._settledNodeIds) ||
                _processedOutcomes.Count != other._processedOutcomes.Count ||
                _domainJournal.Count != other._domainJournal.Count)
            {
                return false;
            }

            for (var index = 0; index < _processedOutcomes.Count; index++)
            {
                var left = _processedOutcomes[index];
                var right = other._processedOutcomes[index];
                if (!Same(left.OutcomeKind, right.OutcomeKind) ||
                    !Same(left.OutcomeCorrelationId, right.OutcomeCorrelationId) ||
                    !Same(left.Fingerprint, right.Fingerprint) ||
                    !Same(left.RoomId, right.RoomId) ||
                    !SameIntList(left.Steps, right.Steps) ||
                    left.ChapterAdvanceKind != right.ChapterAdvanceKind ||
                    left.CompletedChapter != right.CompletedChapter ||
                    left.NextChapter != right.NextChapter)
                {
                    return false;
                }
            }

            for (var index = 0; index < _domainJournal.Count; index++)
            {
                var left = _domainJournal[index];
                var right = other._domainJournal[index];
                if (left.Sequence != right.Sequence || left.Kind != right.Kind ||
                    left.ExpectedRevision != right.ExpectedRevision ||
                    !Same(left.SourceNodeId, right.SourceNodeId) ||
                    !Same(left.NodeId, right.NodeId) ||
                    left.Resolution != right.Resolution || left.Failure != right.Failure ||
                    left.BeforeRevision != right.BeforeRevision ||
                    left.AfterRevision != right.AfterRevision ||
                    left.ChapterAdvanced != right.ChapterAdvanced)
                {
                    return false;
                }
            }

            return true;
        }

        private void ValidateStateRelationships()
        {
            if (_deckStableIds.Count == 0 || _visitedNodeIds.Count == 0 ||
                !Contains(_visitedNodeIds, CurrentNodeId))
            {
                throw new ArgumentException(
                    "Continue state requires a deck and a visited current node.");
            }

            for (var index = 0; index < _settledNodeIds.Count; index++)
            {
                if (!Contains(_visitedNodeIds, _settledNodeIds[index]))
                {
                    throw new ArgumentException(
                        "Every settled node must also be visited.",
                        nameof(SettledNodeIds));
                }
            }

            if (HasActiveRoom)
            {
                if (!Same(ActiveRoomId, CurrentNodeId) ||
                    Contains(_settledNodeIds, ActiveRoomId))
                {
                    throw new ArgumentException(
                        "An active room must be the current visited, unsettled room.",
                        nameof(ActiveRoomId));
                }
            }
            else if (!string.IsNullOrEmpty(ActiveLaunchCorrelationId))
            {
                throw new ArgumentException(
                    "Inactive saves cannot carry active room or launch identity.",
                    nameof(ActiveRoomId));
            }
        }

        private static ReadOnlyCollection<string> CopyOrderedIds(
            IEnumerable<string> source,
            string parameterName,
            bool sort)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in source)
            {
                var id = RequireId(item, parameterName);
                if (sort && !unique.Add(id))
                {
                    throw new ArgumentException(
                        "Stable identity collections cannot contain duplicates.",
                        parameterName);
                }

                copy.Add(id);
            }

            if (sort)
            {
                copy.Sort(StringComparer.Ordinal);
            }

            return new ReadOnlyCollection<string>(copy);
        }

        private static ReadOnlyCollection<OverworldProcessedOutcomeDocument> CopyOutcomes(
            IEnumerable<OverworldProcessedOutcomeDocument> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new List<OverworldProcessedOutcomeDocument>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var outcome in source)
            {
                if (outcome == null || !unique.Add(outcome.OutcomeCorrelationId))
                {
                    throw new ArgumentException(
                        "Consumed outcomes must be non-null with unique identities.",
                        nameof(source));
                }

                copy.Add(new OverworldProcessedOutcomeDocument(
                    outcome.OutcomeKind,
                    outcome.OutcomeCorrelationId,
                    outcome.Fingerprint,
                    outcome.RoomId,
                    outcome.Steps,
                    outcome.ChapterAdvanceKind,
                    outcome.CompletedChapter,
                    outcome.NextChapter));
            }

            copy.Sort((left, right) => StringComparer.Ordinal.Compare(
                left.OutcomeCorrelationId,
                right.OutcomeCorrelationId));
            return new ReadOnlyCollection<OverworldProcessedOutcomeDocument>(copy);
        }

        private static ReadOnlyCollection<OverworldOperationJournalDocument> CopyJournal(
            IEnumerable<OverworldOperationJournalDocument> source)
        {
            var copy = new List<OverworldOperationJournalDocument>();
            var unique = new HashSet<long>();
            foreach (var entry in source ?? new OverworldOperationJournalDocument[0])
            {
                if (entry == null || !unique.Add(entry.Sequence))
                {
                    throw new ArgumentException(
                        "Operation journal entries must be non-null with unique sequences.",
                        nameof(source));
                }

                copy.Add(new OverworldOperationJournalDocument(
                    entry.Sequence,
                    entry.Kind,
                    entry.ExpectedRevision,
                    entry.SourceNodeId,
                    entry.NodeId,
                    entry.Resolution,
                    entry.Failure,
                    entry.BeforeRevision,
                    entry.AfterRevision,
                    entry.ChapterAdvanced));
            }

            copy.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            return new ReadOnlyCollection<OverworldOperationJournalDocument>(copy);
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable identity is required.", parameterName);
            }

            return value.Trim();
        }

        private static string OptionalId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool Contains(IReadOnlyList<string> values, string target)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (Same(values[index], target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SameList(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index++)
            {
                if (!Same(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameIntList(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
