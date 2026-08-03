using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Application.Overworld
{
    public enum OverworldApplicationFailure
    {
        None,
        InvalidCommand,
        InvalidSequence,
        SequenceConflict,
        StaleRevision,
        SourceMismatch,
        MissingRoom,
        NotAdjacent,
        RoomAlreadySettled,
        RoomInProgress,
        NoActiveRoom,
        ActiveRoomMismatch,
        InvalidRoomType,
        RunMismatch,
        RoomMismatch,
        LaunchMismatch,
        OutcomeConflict,
        ChapterAlreadyAdvanced,
        DomainRejected
    }

    public enum EventRoomCompletion
    {
        Completed,
        Cancelled
    }

    public enum ShopRoomCompletion
    {
        Completed,
        Cancelled
    }

    public enum OverworldTransactionStepKind
    {
        AcceptReward,
        ResolveRoom,
        UnlockSuccessors,
        AdvanceChapter,
        RecordDefeat,
        RecordCancellation,
        CommitPersistence
    }

    public enum OverworldChapterAdvanceKind
    {
        None,
        NextChapter,
        FinalChapter
    }

    public sealed class EventRoomOutcome : ISceneTransitionPayload
    {
        public EventRoomOutcome(
            string outcomeCorrelationId,
            string runId,
            MapNodeId roomId,
            EventRoomCompletion completion)
        {
            OutcomeCorrelationId = Required(outcomeCorrelationId, nameof(outcomeCorrelationId));
            RunId = Required(runId, nameof(runId));
            if (string.IsNullOrEmpty(roomId.Value))
            {
                throw new ArgumentException("A room identity is required.", nameof(roomId));
            }

            RoomId = roomId;
            Completion = completion;
        }

        public string OutcomeCorrelationId { get; }
        public string RunId { get; }
        public MapNodeId RoomId { get; }
        public EventRoomCompletion Completion { get; }
        public SceneId TargetScene => SceneId.OutOfBattleShell;
        public string Fingerprint =>
            "event-outcome:" + OutcomeCorrelationId + ":" + RunId + ":" +
            RoomId + ":" + Completion;

        private static string Required(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }
    }

    public sealed class ShopRoomOutcome : ISceneTransitionPayload
    {
        public ShopRoomOutcome(
            string outcomeCorrelationId,
            string runId,
            MapNodeId roomId,
            ShopRoomCompletion completion)
        {
            OutcomeCorrelationId = Required(outcomeCorrelationId, nameof(outcomeCorrelationId));
            RunId = Required(runId, nameof(runId));
            if (string.IsNullOrEmpty(roomId.Value))
            {
                throw new ArgumentException("A room identity is required.", nameof(roomId));
            }

            RoomId = roomId;
            Completion = completion;
        }

        public string OutcomeCorrelationId { get; }
        public string RunId { get; }
        public MapNodeId RoomId { get; }
        public ShopRoomCompletion Completion { get; }
        public SceneId TargetScene => SceneId.OutOfBattleShell;
        public string Fingerprint =>
            "shop-outcome:" + OutcomeCorrelationId + ":" + RunId + ":" +
            RoomId + ":" + Completion;

        private static string Required(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }
    }

    public sealed class OverworldMapNodeProjection
    {
        internal OverworldMapNodeProjection(TimeKey.Domain.Overworld.OverworldMapNode node)
        {
            Id = node.Id;
            Chapter = node.Chapter;
            Layer = node.Layer;
            Slot = node.Slot;
            RoomType = node.RoomType;
        }

        public MapNodeId Id { get; }
        public int Chapter { get; }
        public int Layer { get; }
        public int Slot { get; }
        public OverworldRoomType RoomType { get; }
    }

    public sealed class OverworldMapEdgeProjection
    {
        internal OverworldMapEdgeProjection(MapNodeId from, MapNodeId to)
        {
            From = from;
            To = to;
        }

        public MapNodeId From { get; }
        public MapNodeId To { get; }
    }

    public sealed class OverworldMapProjection
    {
        private readonly ReadOnlyCollection<OverworldMapNodeProjection> _nodes;
        private readonly ReadOnlyCollection<OverworldMapEdgeProjection> _edges;

        internal OverworldMapProjection(
            int seed,
            int chapter,
            MapNodeId entryNodeId,
            MapNodeId bossNodeId,
            string fingerprint,
            IReadOnlyList<OverworldMapNodeProjection> nodes,
            IReadOnlyList<OverworldMapEdgeProjection> edges)
        {
            Seed = seed;
            Chapter = chapter;
            EntryNodeId = entryNodeId;
            BossNodeId = bossNodeId;
            Fingerprint = fingerprint;
            _nodes = new ReadOnlyCollection<OverworldMapNodeProjection>(
                new List<OverworldMapNodeProjection>(nodes));
            _edges = new ReadOnlyCollection<OverworldMapEdgeProjection>(
                new List<OverworldMapEdgeProjection>(edges));
        }

        public int Seed { get; }
        public int Chapter { get; }
        public MapNodeId EntryNodeId { get; }
        public MapNodeId BossNodeId { get; }
        public string Fingerprint { get; }
        public IReadOnlyList<OverworldMapNodeProjection> Nodes => _nodes;
        public IReadOnlyList<OverworldMapEdgeProjection> Edges => _edges;
    }

    public sealed class OverworldChapterAdvanceDecision
    {
        internal OverworldChapterAdvanceDecision(
            OverworldChapterAdvanceKind kind,
            int completedChapter,
            int nextChapter)
        {
            Kind = kind;
            CompletedChapter = completedChapter;
            NextChapter = nextChapter;
        }

        public OverworldChapterAdvanceKind Kind { get; }
        public int CompletedChapter { get; }
        public int NextChapter { get; }
    }

    public sealed class OverworldProcessedOutcomeSnapshot
    {
        private readonly ReadOnlyCollection<OverworldTransactionStepKind> _steps;

        public OverworldProcessedOutcomeSnapshot(
            string outcomeKind,
            string outcomeCorrelationId,
            string fingerprint,
            string roomId,
            IReadOnlyList<OverworldTransactionStepKind> steps,
            OverworldChapterAdvanceKind chapterAdvanceKind,
            int completedChapter,
            int nextChapter)
        {
            OutcomeKind = outcomeKind;
            OutcomeCorrelationId = outcomeCorrelationId;
            Fingerprint = fingerprint;
            RoomId = roomId ?? string.Empty;
            _steps = new ReadOnlyCollection<OverworldTransactionStepKind>(
                new List<OverworldTransactionStepKind>(
                    steps ?? Array.Empty<OverworldTransactionStepKind>()));
            ChapterAdvanceKind = chapterAdvanceKind;
            CompletedChapter = completedChapter;
            NextChapter = nextChapter;
        }

        public string OutcomeKind { get; }
        public string OutcomeCorrelationId { get; }
        public string Fingerprint { get; }
        public string RoomId { get; }
        public IReadOnlyList<OverworldTransactionStepKind> Steps => _steps;
        public OverworldChapterAdvanceKind ChapterAdvanceKind { get; }
        public int CompletedChapter { get; }
        public int NextChapter { get; }
    }

    public sealed class OverworldOperationJournalPersistenceSnapshot
    {
        public OverworldOperationJournalPersistenceSnapshot(
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
            Sequence = sequence;
            Kind = kind;
            ExpectedRevision = expectedRevision;
            SourceNodeId = sourceNodeId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            Resolution = resolution;
            Failure = failure;
            BeforeRevision = beforeRevision;
            AfterRevision = afterRevision;
            ChapterAdvanced = chapterAdvanced;
        }

        public long Sequence { get; }
        public int Kind { get; }
        public int ExpectedRevision { get; }
        public string SourceNodeId { get; }
        public string NodeId { get; }
        public int Resolution { get; }
        public int Failure { get; }
        public int BeforeRevision { get; }
        public int AfterRevision { get; }
        public bool ChapterAdvanced { get; }
    }

    public sealed class OverworldPersistenceSnapshot
    {
        public const int CurrentSchemaVersion = 1;
        public const int CurrentMapConfigVersion = 1;

        private readonly ReadOnlyCollection<string> _deckStableIds;
        private readonly ReadOnlyCollection<string> _visitedNodeIds;
        private readonly ReadOnlyCollection<string> _settledNodeIds;
        private readonly ReadOnlyCollection<OverworldProcessedOutcomeSnapshot> _processedOutcomes;
        private readonly ReadOnlyCollection<OverworldOperationJournalPersistenceSnapshot> _domainJournal;

        public OverworldPersistenceSnapshot(
            string runId,
            int runStartKind,
            int runSeed,
            string seedText,
            int chapter,
            int finalChapter,
            int era,
            int phase,
            int timecoins,
            string characterId,
            IReadOnlyList<string> deckStableIds,
            int intermediateLayerCount,
            int minimumNodesPerLayer,
            int maximumNodesPerLayer,
            string mapFingerprint,
            string currentNodeId,
            string activeRoomId,
            string activeLaunchCorrelationId,
            bool chapterCompleted,
            int chapterAdvanceCount,
            int domainRevision,
            long operationSequenceCursor,
            int persistenceRevision,
            IReadOnlyList<string> visitedNodeIds,
            IReadOnlyList<string> settledNodeIds,
            IReadOnlyList<OverworldProcessedOutcomeSnapshot> processedOutcomes,
            IReadOnlyList<OverworldOperationJournalPersistenceSnapshot> domainJournal)
        {
            SchemaVersion = CurrentSchemaVersion;
            MapConfigVersion = CurrentMapConfigVersion;
            RunId = runId;
            RunStartKind = runStartKind;
            RunSeed = runSeed;
            SeedText = seedText;
            Chapter = chapter;
            FinalChapter = finalChapter;
            Era = era;
            Phase = phase;
            Timecoins = timecoins;
            CharacterId = characterId;
            IntermediateLayerCount = intermediateLayerCount;
            MinimumNodesPerLayer = minimumNodesPerLayer;
            MaximumNodesPerLayer = maximumNodesPerLayer;
            MapFingerprint = mapFingerprint;
            CurrentNodeId = currentNodeId;
            ActiveRoomId = activeRoomId;
            ActiveLaunchCorrelationId = activeLaunchCorrelationId;
            ChapterCompleted = chapterCompleted;
            ChapterAdvanceCount = chapterAdvanceCount;
            DomainRevision = domainRevision;
            OperationSequenceCursor = operationSequenceCursor;
            PersistenceRevision = persistenceRevision;
            _deckStableIds = Copy(deckStableIds);
            _visitedNodeIds = Copy(visitedNodeIds);
            _settledNodeIds = Copy(settledNodeIds);
            _processedOutcomes = new ReadOnlyCollection<OverworldProcessedOutcomeSnapshot>(
                new List<OverworldProcessedOutcomeSnapshot>(processedOutcomes));
            _domainJournal = new ReadOnlyCollection<OverworldOperationJournalPersistenceSnapshot>(
                new List<OverworldOperationJournalPersistenceSnapshot>(domainJournal));
        }

        public int SchemaVersion { get; }
        public int MapConfigVersion { get; }
        public string RunId { get; }
        public int RunStartKind { get; }
        public int RunSeed { get; }
        public string SeedText { get; }
        public int Chapter { get; }
        public int FinalChapter { get; }
        public int Era { get; }
        public int Phase { get; }
        public int Timecoins { get; }
        public string CharacterId { get; }
        public IReadOnlyList<string> DeckStableIds => _deckStableIds;
        public int IntermediateLayerCount { get; }
        public int MinimumNodesPerLayer { get; }
        public int MaximumNodesPerLayer { get; }
        public string MapFingerprint { get; }
        public string CurrentNodeId { get; }
        public string ActiveRoomId { get; }
        public string ActiveLaunchCorrelationId { get; }
        public bool ChapterCompleted { get; }
        public int ChapterAdvanceCount { get; }
        public int DomainRevision { get; }
        public long OperationSequenceCursor { get; }
        public int PersistenceRevision { get; }
        public IReadOnlyList<string> VisitedNodeIds => _visitedNodeIds;
        public IReadOnlyList<string> SettledNodeIds => _settledNodeIds;
        public IReadOnlyList<OverworldProcessedOutcomeSnapshot> ProcessedOutcomes => _processedOutcomes;
        public IReadOnlyList<OverworldOperationJournalPersistenceSnapshot> DomainJournal =>
            _domainJournal;

        private static ReadOnlyCollection<string> Copy(IReadOnlyList<string> source)
        {
            return new ReadOnlyCollection<string>(new List<string>(source));
        }
    }

    public sealed class OverworldCommitPlan
    {
        private readonly ReadOnlyCollection<OverworldTransactionStepKind> _steps;

        internal OverworldCommitPlan(
            long operationSequence,
            MapNodeId roomId,
            IReadOnlyList<OverworldTransactionStepKind> steps,
            OverworldChapterAdvanceDecision chapterDecision,
            OverworldPersistenceSnapshot persistenceSnapshot)
        {
            OperationSequence = operationSequence;
            RoomId = roomId;
            _steps = new ReadOnlyCollection<OverworldTransactionStepKind>(
                new List<OverworldTransactionStepKind>(steps));
            ChapterDecision = chapterDecision;
            PersistenceSnapshot = persistenceSnapshot;
        }

        public long OperationSequence { get; }
        public MapNodeId RoomId { get; }
        public IReadOnlyList<OverworldTransactionStepKind> Steps => _steps;
        public OverworldChapterAdvanceDecision ChapterDecision { get; }
        public OverworldPersistenceSnapshot PersistenceSnapshot { get; }
    }

    public sealed class OverworldCombatLaunchResult
    {
        internal OverworldCombatLaunchResult(
            OverworldApplicationFailure failure,
            OverworldOperationFailure domainFailure,
            bool wasAlreadyApplied,
            CombatLaunchPayload launch,
            OverworldPersistenceSnapshot persistenceSnapshot)
        {
            Failure = failure;
            DomainFailure = domainFailure;
            WasAlreadyApplied = wasAlreadyApplied;
            Launch = launch;
            PersistenceSnapshot = persistenceSnapshot;
        }

        public bool Succeeded => Failure == OverworldApplicationFailure.None;
        public OverworldApplicationFailure Failure { get; }
        public OverworldOperationFailure DomainFailure { get; }
        public bool WasAlreadyApplied { get; }
        public CombatLaunchPayload Launch { get; }
        public OverworldPersistenceSnapshot PersistenceSnapshot { get; }

        internal OverworldCombatLaunchResult AsReplay()
        {
            return new OverworldCombatLaunchResult(
                Failure,
                DomainFailure,
                wasAlreadyApplied: Succeeded,
                Launch,
                PersistenceSnapshot);
        }
    }

    public sealed class OverworldRoomEntryResult
    {
        internal OverworldRoomEntryResult(
            OverworldApplicationFailure failure,
            OverworldOperationFailure domainFailure,
            bool wasAlreadyApplied,
            MapNodeId roomId,
            OverworldRoomType roomType,
            OverworldPersistenceSnapshot persistenceSnapshot)
        {
            Failure = failure;
            DomainFailure = domainFailure;
            WasAlreadyApplied = wasAlreadyApplied;
            RoomId = roomId;
            RoomType = roomType;
            PersistenceSnapshot = persistenceSnapshot;
        }

        public bool Succeeded => Failure == OverworldApplicationFailure.None;
        public OverworldApplicationFailure Failure { get; }
        public OverworldOperationFailure DomainFailure { get; }
        public bool WasAlreadyApplied { get; }
        public MapNodeId RoomId { get; }
        public OverworldRoomType RoomType { get; }
        public OverworldPersistenceSnapshot PersistenceSnapshot { get; }

        internal OverworldRoomEntryResult AsReplay()
        {
            return new OverworldRoomEntryResult(
                Failure,
                DomainFailure,
                wasAlreadyApplied: Succeeded,
                RoomId,
                RoomType,
                PersistenceSnapshot);
        }
    }

    public sealed class OverworldRoomOutcomeApplyResult
    {
        internal OverworldRoomOutcomeApplyResult(
            OverworldApplicationFailure failure,
            OverworldOperationFailure domainFailure,
            bool wasAlreadyApplied,
            OverworldCommitPlan commitPlan)
        {
            Failure = failure;
            DomainFailure = domainFailure;
            WasAlreadyApplied = wasAlreadyApplied;
            CommitPlan = commitPlan;
        }

        public bool Succeeded => Failure == OverworldApplicationFailure.None;
        public OverworldApplicationFailure Failure { get; }
        public OverworldOperationFailure DomainFailure { get; }
        public bool WasAlreadyApplied { get; }
        public OverworldCommitPlan CommitPlan { get; }

        internal OverworldRoomOutcomeApplyResult AsReplay()
        {
            return new OverworldRoomOutcomeApplyResult(
                Failure,
                DomainFailure,
                wasAlreadyApplied: Succeeded,
                CommitPlan);
        }
    }
}
