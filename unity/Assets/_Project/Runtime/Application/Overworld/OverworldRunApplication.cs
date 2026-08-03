using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Application.Overworld
{
    public sealed class OverworldRunApplication
    {
        private static readonly string[] GateCShopCardCatalog =
        {
            "lighting", "earthquake", "wind", "recover", "tower", "poison", "tornado"
        };

        private readonly OverworldMapGenerationConfig _config;
        private readonly OverworldMapDefinition _map;
        private OverworldChapterState _chapterState;
        private readonly int _finalChapter;
        private readonly RunStartKind _runStartKind;
        private readonly string _seedText;
        private readonly string _runId;
        private readonly int _runSeed;
        private readonly int _chapter;
        private readonly string _characterId;
        private readonly Dictionary<long, string> _sequenceFingerprints =
            new Dictionary<long, string>();
        private readonly Dictionary<long, OverworldCombatLaunchResult> _combatLaunchResults =
            new Dictionary<long, OverworldCombatLaunchResult>();
        private readonly Dictionary<long, OverworldRoomEntryResult> _roomEntryResults =
            new Dictionary<long, OverworldRoomEntryResult>();
        private readonly Dictionary<long, OverworldRoomOutcomeApplyResult> _outcomeResults =
            new Dictionary<long, OverworldRoomOutcomeApplyResult>();
        private readonly Dictionary<string, ProcessedOutcomeRecord> _processedOutcomes =
            new Dictionary<string, ProcessedOutcomeRecord>(StringComparer.Ordinal);

        private ReadOnlyCollection<string> _deckStableIds;
        private CombatLaunchPayload _activeCombatLaunch;
        private int _era;
        private int _phase;
        private int _timecoins;
        private long _operationSequenceCursor;
        private int _persistenceRevision;

        public OverworldRunApplication(
            RunStartPayload start,
            OverworldMapGenerationConfig config,
            int finalChapter)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.Chapter != start.Chapter)
            {
                throw new ArgumentException(
                    "The map chapter must match the run chapter.",
                    nameof(config));
            }

            if (finalChapter < start.Chapter)
            {
                throw new ArgumentOutOfRangeException(nameof(finalChapter));
            }

            _config = config;
            _finalChapter = finalChapter;
            _runStartKind = start.Kind;
            _seedText = start.SeedText;
            _runId = start.RunId;
            _runSeed = start.RunSeed;
            _chapter = start.Chapter;
            _characterId = start.CharacterId;
            _era = start.Era;
            _phase = start.Phase;
            _timecoins = start.Timecoins;
            _deckStableIds = Copy(start.DeckStableIds);
            _map = new DeterministicOverworldMapGenerator().Generate(start.RunSeed, config);
            _chapterState = new OverworldChapterState(_map);
            Map = CreateMapProjection(_map, config);
        }

        private OverworldRunApplication(OverworldRunApplication source)
        {
            _config = source._config;
            _map = source._map;
            _chapterState = OverworldChapterState.Restore(
                _map,
                source._chapterState.ExportState());
            _finalChapter = source._finalChapter;
            _runStartKind = source._runStartKind;
            _seedText = source._seedText;
            _runId = source._runId;
            _runSeed = source._runSeed;
            _chapter = source._chapter;
            _characterId = source._characterId;
            _deckStableIds = Copy(source._deckStableIds);
            _activeCombatLaunch = source._activeCombatLaunch;
            _era = source._era;
            _phase = source._phase;
            _timecoins = source._timecoins;
            _operationSequenceCursor = source._operationSequenceCursor;
            _persistenceRevision = source._persistenceRevision;
            Map = source.Map;
            foreach (var pair in source._sequenceFingerprints)
            {
                _sequenceFingerprints.Add(pair.Key, pair.Value);
            }

            foreach (var pair in source._combatLaunchResults)
            {
                _combatLaunchResults.Add(pair.Key, pair.Value);
            }

            foreach (var pair in source._roomEntryResults)
            {
                _roomEntryResults.Add(pair.Key, pair.Value);
            }

            foreach (var pair in source._outcomeResults)
            {
                _outcomeResults.Add(pair.Key, pair.Value);
            }

            foreach (var pair in source._processedOutcomes)
            {
                _processedOutcomes.Add(pair.Key, pair.Value.Copy());
            }
        }

        public OverworldMapProjection Map { get; }

        public int Revision => _chapterState.Revision;

        public long OperationSequenceCursor => _operationSequenceCursor;

        public OverworldChapterSnapshot ChapterSnapshot => _chapterState.Snapshot();

        public static OverworldRunApplication Restore(OverworldPersistenceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (!string.IsNullOrEmpty(snapshot.ActiveRoomId) ||
                !string.IsNullOrEmpty(snapshot.ActiveLaunchCorrelationId))
            {
                throw new InvalidOperationException(
                    "Continue only restores fully committed room boundaries.");
            }

            var start = new RunStartPayload(
                (RunStartKind)snapshot.RunStartKind,
                snapshot.RunId,
                snapshot.RunSeed,
                snapshot.SeedText,
                snapshot.Chapter,
                snapshot.Era,
                snapshot.Phase,
                snapshot.Timecoins,
                snapshot.CharacterId,
                snapshot.DeckStableIds);
            var config = new OverworldMapGenerationConfig(
                snapshot.Chapter,
                snapshot.IntermediateLayerCount,
                snapshot.MinimumNodesPerLayer,
                snapshot.MaximumNodesPerLayer);
            var restored = new OverworldRunApplication(start, config, snapshot.FinalChapter);
            if (!string.Equals(
                    restored.Map.Fingerprint,
                    snapshot.MapFingerprint,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The saved map fingerprint does not match the deterministic generator.");
            }

            var visited = new List<MapNodeId>(snapshot.VisitedNodeIds.Count);
            foreach (var nodeId in snapshot.VisitedNodeIds)
            {
                visited.Add(new MapNodeId(nodeId));
            }

            var settled = new List<MapNodeId>(snapshot.SettledNodeIds.Count);
            foreach (var nodeId in snapshot.SettledNodeIds)
            {
                settled.Add(new MapNodeId(nodeId));
            }

            var journal = new List<OverworldOperationJournalSnapshot>(
                snapshot.DomainJournal.Count);
            foreach (var entry in snapshot.DomainJournal)
            {
                journal.Add(new OverworldOperationJournalSnapshot(
                    entry.Sequence,
                    (OverworldOperationKind)entry.Kind,
                    entry.ExpectedRevision,
                    OptionalNodeId(entry.SourceNodeId),
                    new MapNodeId(entry.NodeId),
                    (OverworldResolutionKind)entry.Resolution,
                    (OverworldOperationFailure)entry.Failure,
                    entry.BeforeRevision,
                    entry.AfterRevision,
                    entry.ChapterAdvanced));
            }

            restored._chapterState = OverworldChapterState.Restore(
                restored._map,
                new OverworldChapterRestoreState(
                    snapshot.DomainRevision,
                    new MapNodeId(snapshot.CurrentNodeId),
                    hasActiveRoom: false,
                    activeRoomNodeId: default(MapNodeId),
                    snapshot.ChapterCompleted,
                    snapshot.ChapterAdvanceCount,
                    visited,
                    settled,
                    journal));
            restored._operationSequenceCursor = snapshot.OperationSequenceCursor;
            restored._persistenceRevision = snapshot.PersistenceRevision;
            foreach (var outcome in snapshot.ProcessedOutcomes)
            {
                var steps = new List<OverworldTransactionStepKind>(outcome.Steps.Count);
                foreach (var step in outcome.Steps)
                {
                    steps.Add(step);
                }

                restored._processedOutcomes.Add(
                    outcome.OutcomeCorrelationId,
                    new ProcessedOutcomeRecord(
                        outcome.OutcomeKind,
                        outcome.OutcomeCorrelationId,
                        outcome.Fingerprint,
                        new MapNodeId(
                            string.IsNullOrEmpty(outcome.RoomId)
                                ? snapshot.CurrentNodeId
                                : outcome.RoomId),
                        steps,
                        new OverworldChapterAdvanceDecision(
                            outcome.ChapterAdvanceKind,
                            outcome.CompletedChapter,
                            outcome.NextChapter)));
            }

            return restored;
        }

        public OverworldRunApplication Copy()
        {
            return new OverworldRunApplication(this);
        }

        public OverworldRunApplication CreateNextChapter(int nextChapter)
        {
            if (!_chapterState.ChapterCompleted || nextChapter != _chapter + 1 ||
                nextChapter > _finalChapter)
            {
                throw new InvalidOperationException(
                    "The current run cannot advance to the requested chapter.");
            }

            var start = new RunStartPayload(
                _runStartKind,
                _runId,
                _runSeed,
                _seedText,
                nextChapter,
                _era,
                _phase,
                _timecoins,
                _characterId,
                _deckStableIds);
            var next = new OverworldRunApplication(
                start,
                new OverworldMapGenerationConfig(
                    nextChapter,
                    _config.IntermediateLayerCount,
                    _config.MinimumNodesPerLayer,
                    _config.MaximumNodesPerLayer),
                _finalChapter);
            next._operationSequenceCursor = _operationSequenceCursor;
            next._persistenceRevision = _persistenceRevision;
            foreach (var pair in _processedOutcomes)
            {
                next._processedOutcomes.Add(pair.Key, pair.Value.Copy());
            }

            return next;
        }

        public IReadOnlyList<MapNodeId> GetAvailableRoomIds()
        {
            var result = new List<MapNodeId>();
            foreach (var node in _chapterState.Snapshot().Nodes)
            {
                if (node.IsAvailable)
                {
                    result.Add(node.Id);
                }
            }

            return new ReadOnlyCollection<MapNodeId>(result);
        }

        public OverworldRoomType? GetRoomType(MapNodeId roomId)
        {
            var node = _map.GetNode(roomId);
            return node == null ? (OverworldRoomType?)null : node.RoomType;
        }

        public OverworldCombatLaunchResult TryBeginCombat(
            EnterOverworldRoomCommand command,
            string launchCorrelationId,
            string battleTag,
            int battleSeed)
        {
            if (command == null)
            {
                return CombatLaunchFailed(OverworldApplicationFailure.InvalidCommand);
            }

            if (command.Sequence <= 0)
            {
                return CombatLaunchFailed(OverworldApplicationFailure.InvalidSequence);
            }

            var fingerprint =
                "combat-entry|" + command.ExpectedRevision + "|" +
                command.SourceNodeId + "|" + command.TargetNodeId + "|" +
                launchCorrelationId + "|" + battleTag + "|" + battleSeed;
            OverworldCombatLaunchResult replay;
            var sequenceCheck = TryGetCombatLaunchReplay(command.Sequence, fingerprint, out replay);
            if (sequenceCheck != SequenceCheck.New)
            {
                return sequenceCheck == SequenceCheck.Replay
                    ? replay
                    : CombatLaunchFailed(OverworldApplicationFailure.SequenceConflict);
            }

            OverworldCombatLaunchResult result;
            if (string.IsNullOrWhiteSpace(launchCorrelationId) ||
                string.IsNullOrWhiteSpace(battleTag))
            {
                result = CombatLaunchFailed(OverworldApplicationFailure.InvalidCommand);
            }
            else
            {
                var node = _map.GetNode(command.TargetNodeId);
                if (node == null)
                {
                    result = CombatLaunchFailed(OverworldApplicationFailure.MissingRoom);
                }
                else if (!IsCombatRoom(node.RoomType))
                {
                    result = CombatLaunchFailed(OverworldApplicationFailure.InvalidRoomType);
                }
                else
                {
                    var launch = new CombatLaunchPayload(
                        launchCorrelationId,
                        _runId,
                        _runSeed,
                        _chapter,
                        _era,
                        _phase,
                        command.TargetNodeId.Value,
                        _characterId,
                        _timecoins,
                        battleTag,
                        battleSeed,
                        _deckStableIds);
                    var domainResult = _chapterState.EnterRoom(command);
                    if (!domainResult.Succeeded)
                    {
                        result = CombatLaunchFailed(
                            MapFailure(domainResult.Failure),
                            domainResult.Failure);
                    }
                    else
                    {
                        _activeCombatLaunch = launch;
                        MarkCommitted(command.Sequence);
                        result = new OverworldCombatLaunchResult(
                            OverworldApplicationFailure.None,
                            OverworldOperationFailure.None,
                            wasAlreadyApplied: false,
                            launch,
                            CreatePersistenceSnapshot());
                    }
                }
            }

            RecordSequence(command.Sequence, fingerprint);
            _combatLaunchResults.Add(command.Sequence, result);
            return result;
        }

        public OverworldRoomEntryResult TryEnterEventRoom(EnterOverworldRoomCommand command)
        {
            return TryEnterNonCombatRoom(command, OverworldRoomType.Event, "event-entry");
        }

        public OverworldRoomEntryResult TryEnterShopRoom(EnterOverworldRoomCommand command)
        {
            return TryEnterNonCombatRoom(command, OverworldRoomType.Shop, "shop-entry");
        }

        public OverworldRoomOutcomeApplyResult TryApplyCombatOutcome(
            long sequence,
            int expectedRevision,
            CombatOutcome outcome)
        {
            if (outcome == null)
            {
                return OutcomeFailed(OverworldApplicationFailure.InvalidCommand);
            }

            if (sequence <= 0)
            {
                return OutcomeFailed(OverworldApplicationFailure.InvalidSequence);
            }

            var fingerprint =
                "combat-outcome|" + expectedRevision + "|" + outcome.Fingerprint;
            OverworldRoomOutcomeApplyResult replay;
            var sequenceCheck = TryGetOutcomeReplay(sequence, fingerprint, out replay);
            if (sequenceCheck != SequenceCheck.New)
            {
                return sequenceCheck == SequenceCheck.Replay
                    ? replay
                    : OutcomeFailed(OverworldApplicationFailure.SequenceConflict);
            }

            var outcomeReplay = TryGetProcessedOutcomeReplay(
                sequence,
                fingerprint,
                "combat",
                outcome.OutcomeCorrelationId,
                outcome.Fingerprint);
            if (outcomeReplay != null)
            {
                return outcomeReplay;
            }

            OverworldRoomOutcomeApplyResult result;
            if (outcome.RunId != _runId)
            {
                result = OutcomeFailed(OverworldApplicationFailure.RunMismatch);
            }
            else if (_activeCombatLaunch == null)
            {
                result = OutcomeFailed(OverworldApplicationFailure.NoActiveRoom);
            }
            else if (outcome.RoomId != _activeCombatLaunch.RoomId)
            {
                result = OutcomeFailed(OverworldApplicationFailure.RoomMismatch);
            }
            else if (outcome.LaunchCorrelationId != _activeCombatLaunch.LaunchCorrelationId)
            {
                result = OutcomeFailed(OverworldApplicationFailure.LaunchMismatch);
            }
            else
            {
                var roomId = new MapNodeId(outcome.RoomId);
                var resolution = outcome.ReturnPayload.Outcome == BattleOutcome.VictorySettlement
                    ? OverworldResolutionKind.Victory
                    : OverworldResolutionKind.Defeat;
                var domainResult = _chapterState.ResolveRoom(new ResolveOverworldRoomCommand(
                    sequence,
                    expectedRevision,
                    roomId,
                    resolution));
                if (!domainResult.Succeeded)
                {
                    result = OutcomeFailed(
                        MapFailure(domainResult.Failure),
                        domainResult.Failure);
                }
                else
                {
                    _era = outcome.ReturnPayload.Era;
                    _phase = outcome.ReturnPayload.Phase;
                    _timecoins = outcome.ReturnPayload.Timecoins;
                    _deckStableIds = Copy(outcome.ReturnPayload.DeckStableIds);
                    _activeCombatLaunch = null;
                    MarkCommitted(sequence);
                    var decision = CreateChapterDecision(domainResult.ChapterAdvanced);
                    var steps = resolution == OverworldResolutionKind.Victory
                        ? VictorySteps(domainResult.ChapterAdvanced)
                        : new[]
                        {
                            OverworldTransactionStepKind.RecordDefeat,
                            OverworldTransactionStepKind.CommitPersistence
                        };
                    RecordProcessedOutcome(
                        "combat",
                        outcome.OutcomeCorrelationId,
                        outcome.Fingerprint);
                    result = OutcomeSucceeded(
                        sequence,
                        roomId,
                        steps,
                        decision,
                        outcome.OutcomeCorrelationId);
                }
            }

            RecordSequence(sequence, fingerprint);
            _outcomeResults.Add(sequence, result);
            return result;
        }

        public OverworldRoomOutcomeApplyResult TryApplyEventOutcome(
            long sequence,
            int expectedRevision,
            EventRoomOutcome outcome)
        {
            if (outcome == null)
            {
                return OutcomeFailed(OverworldApplicationFailure.InvalidCommand);
            }

            return TryApplyNonCombatOutcome(
                sequence,
                expectedRevision,
                "event",
                outcome.OutcomeCorrelationId,
                outcome.Fingerprint,
                outcome.RunId,
                outcome.RoomId,
                OverworldRoomType.Event,
                outcome.Completion == EventRoomCompletion.Completed,
                string.Empty,
                0);
        }

        public OverworldRoomOutcomeApplyResult TryApplyShopOutcome(
            long sequence,
            int expectedRevision,
            ShopRoomOutcome outcome)
        {
            if (outcome == null)
            {
                return OutcomeFailed(OverworldApplicationFailure.InvalidCommand);
            }

            return TryApplyNonCombatOutcome(
                sequence,
                expectedRevision,
                "shop",
                outcome.OutcomeCorrelationId,
                outcome.Fingerprint,
                outcome.RunId,
                outcome.RoomId,
                OverworldRoomType.Shop,
                outcome.Completion == ShopRoomCompletion.Completed,
                outcome.PurchasedCardStableId,
                outcome.TimecoinCost);
        }

        public OverworldShopOffer GetShopOffer(MapNodeId roomId)
        {
            var node = _map.GetNode(roomId);
            if (node == null || node.RoomType != OverworldRoomType.Shop)
            {
                throw new InvalidOperationException("The requested node is not a shop room.");
            }

            unchecked
            {
                var hash = (uint)_runSeed;
                for (var index = 0; index < roomId.Value.Length; index++)
                {
                    hash ^= roomId.Value[index];
                    hash *= 16777619u;
                }

                var card = GateCShopCardCatalog[
                    hash % (uint)GateCShopCardCatalog.Length];
                return new OverworldShopOffer(card, 50);
            }
        }

        public OverworldPersistenceSnapshot CreatePersistenceSnapshot()
        {
            var domain = _chapterState.Snapshot();
            var restore = _chapterState.ExportState();
            var visited = StableIds(domain.VisitedNodeIds);
            var settled = StableIds(domain.SettledNodeIds);
            var outcomes = new List<OverworldProcessedOutcomeSnapshot>();
            var outcomeIds = new List<string>(_processedOutcomes.Keys);
            outcomeIds.Sort(StringComparer.Ordinal);
            foreach (var outcomeId in outcomeIds)
            {
                var outcome = _processedOutcomes[outcomeId];
                outcomes.Add(new OverworldProcessedOutcomeSnapshot(
                    outcome.Kind,
                    outcome.CorrelationId,
                    outcome.Fingerprint,
                    outcome.RoomId.Value,
                    outcome.Steps,
                    outcome.Decision.Kind,
                    outcome.Decision.CompletedChapter,
                    outcome.Decision.NextChapter));
            }

            var journal = new List<OverworldOperationJournalPersistenceSnapshot>(
                restore.Journal.Count);
            foreach (var entry in restore.Journal)
            {
                journal.Add(new OverworldOperationJournalPersistenceSnapshot(
                    entry.Sequence,
                    (int)entry.Kind,
                    entry.ExpectedRevision,
                    entry.SourceNodeId.Value,
                    entry.NodeId.Value,
                    (int)entry.Resolution,
                    (int)entry.Failure,
                    entry.BeforeRevision,
                    entry.AfterRevision,
                    entry.ChapterAdvanced));
            }

            return new OverworldPersistenceSnapshot(
                _runId,
                (int)_runStartKind,
                _runSeed,
                _seedText,
                _chapter,
                _finalChapter,
                _era,
                _phase,
                _timecoins,
                _characterId,
                _deckStableIds,
                _config.IntermediateLayerCount,
                _config.MinimumNodesPerLayer,
                _config.MaximumNodesPerLayer,
                Map.Fingerprint,
                domain.CurrentNodeId.Value,
                domain.HasActiveRoom ? domain.ActiveRoomNodeId.Value : string.Empty,
                _activeCombatLaunch == null
                    ? string.Empty
                    : _activeCombatLaunch.LaunchCorrelationId,
                domain.ChapterCompleted,
                domain.ChapterAdvanceCount,
                domain.Revision,
                _operationSequenceCursor,
                _persistenceRevision,
                visited,
                settled,
                outcomes,
                journal);
        }

        private OverworldRoomEntryResult TryEnterNonCombatRoom(
            EnterOverworldRoomCommand command,
            OverworldRoomType expectedRoomType,
            string operationKind)
        {
            if (command == null)
            {
                return RoomEntryFailed(OverworldApplicationFailure.InvalidCommand);
            }

            if (command.Sequence <= 0)
            {
                return RoomEntryFailed(OverworldApplicationFailure.InvalidSequence);
            }

            var fingerprint =
                operationKind + "|" + command.ExpectedRevision + "|" +
                command.SourceNodeId + "|" + command.TargetNodeId;
            OverworldRoomEntryResult replay;
            var sequenceCheck = TryGetRoomEntryReplay(command.Sequence, fingerprint, out replay);
            if (sequenceCheck != SequenceCheck.New)
            {
                return sequenceCheck == SequenceCheck.Replay
                    ? replay
                    : RoomEntryFailed(OverworldApplicationFailure.SequenceConflict);
            }

            OverworldRoomEntryResult result;
            var node = _map.GetNode(command.TargetNodeId);
            if (node == null)
            {
                result = RoomEntryFailed(OverworldApplicationFailure.MissingRoom);
            }
            else if (node.RoomType != expectedRoomType)
            {
                result = RoomEntryFailed(OverworldApplicationFailure.InvalidRoomType);
            }
            else
            {
                var domainResult = _chapterState.EnterRoom(command);
                if (!domainResult.Succeeded)
                {
                    result = RoomEntryFailed(
                        MapFailure(domainResult.Failure),
                        domainResult.Failure);
                }
                else
                {
                    MarkCommitted(command.Sequence);
                    result = new OverworldRoomEntryResult(
                        OverworldApplicationFailure.None,
                        OverworldOperationFailure.None,
                        wasAlreadyApplied: false,
                        command.TargetNodeId,
                        node.RoomType,
                        CreatePersistenceSnapshot());
                }
            }

            RecordSequence(command.Sequence, fingerprint);
            _roomEntryResults.Add(command.Sequence, result);
            return result;
        }

        private OverworldRoomOutcomeApplyResult TryApplyNonCombatOutcome(
            long sequence,
            int expectedRevision,
            string outcomeKind,
            string outcomeCorrelationId,
            string outcomeFingerprint,
            string runId,
            MapNodeId roomId,
            OverworldRoomType expectedRoomType,
            bool completed,
            string purchasedCardStableId,
            int timecoinCost)
        {
            if (sequence <= 0)
            {
                return OutcomeFailed(OverworldApplicationFailure.InvalidSequence);
            }

            var fingerprint =
                outcomeKind + "-outcome|" + expectedRevision + "|" + outcomeFingerprint;
            OverworldRoomOutcomeApplyResult replay;
            var sequenceCheck = TryGetOutcomeReplay(sequence, fingerprint, out replay);
            if (sequenceCheck != SequenceCheck.New)
            {
                return sequenceCheck == SequenceCheck.Replay
                    ? replay
                    : OutcomeFailed(OverworldApplicationFailure.SequenceConflict);
            }

            var outcomeReplay = TryGetProcessedOutcomeReplay(
                sequence,
                fingerprint,
                outcomeKind,
                outcomeCorrelationId,
                outcomeFingerprint);
            if (outcomeReplay != null)
            {
                return outcomeReplay;
            }

            OverworldRoomOutcomeApplyResult result;
            if (runId != _runId)
            {
                result = OutcomeFailed(OverworldApplicationFailure.RunMismatch);
            }
            else
            {
                var node = _map.GetNode(roomId);
                if (node == null)
                {
                    result = OutcomeFailed(OverworldApplicationFailure.MissingRoom);
                }
                else if (node.RoomType != expectedRoomType)
                {
                    result = OutcomeFailed(OverworldApplicationFailure.InvalidRoomType);
                }
                else if (timecoinCost < 0 ||
                         (timecoinCost > 0 && string.IsNullOrWhiteSpace(purchasedCardStableId)))
                {
                    result = OutcomeFailed(OverworldApplicationFailure.InvalidCommand);
                }
                else if (timecoinCost > _timecoins)
                {
                    result = OutcomeFailed(OverworldApplicationFailure.InsufficientResources);
                }
                else
                {
                    var domainResult = _chapterState.ResolveRoom(
                        new ResolveOverworldRoomCommand(
                            sequence,
                            expectedRevision,
                            roomId,
                            completed
                                ? OverworldResolutionKind.Completed
                                : OverworldResolutionKind.Cancelled));
                    if (!domainResult.Succeeded)
                    {
                        result = OutcomeFailed(
                            MapFailure(domainResult.Failure),
                            domainResult.Failure);
                    }
                    else
                    {
                        var purchased = expectedRoomType == OverworldRoomType.Shop &&
                            completed && timecoinCost > 0;
                        if (purchased)
                        {
                            _timecoins -= timecoinCost;
                            _deckStableIds = Append(_deckStableIds, purchasedCardStableId);
                        }

                        MarkCommitted(sequence);
                        RecordProcessedOutcome(
                            outcomeKind,
                            outcomeCorrelationId,
                            outcomeFingerprint);
                        var steps = completed
                            ? purchased
                                ? new[]
                                {
                                    OverworldTransactionStepKind.SpendTimecoins,
                                    OverworldTransactionStepKind.AddCardToDeck,
                                    OverworldTransactionStepKind.ResolveRoom,
                                    OverworldTransactionStepKind.UnlockSuccessors,
                                    OverworldTransactionStepKind.CommitPersistence
                                }
                                : new[]
                                {
                                    OverworldTransactionStepKind.ResolveRoom,
                                    OverworldTransactionStepKind.UnlockSuccessors,
                                    OverworldTransactionStepKind.CommitPersistence
                                }
                            : new[]
                            {
                                OverworldTransactionStepKind.RecordCancellation,
                                OverworldTransactionStepKind.CommitPersistence
                            };
                        result = OutcomeSucceeded(
                            sequence,
                            roomId,
                            steps,
                            CreateChapterDecision(chapterAdvanced: false),
                            outcomeCorrelationId);
                    }
                }
            }

            RecordSequence(sequence, fingerprint);
            _outcomeResults.Add(sequence, result);
            return result;
        }

        private OverworldRoomOutcomeApplyResult TryGetProcessedOutcomeReplay(
            long sequence,
            string sequenceFingerprint,
            string outcomeKind,
            string outcomeCorrelationId,
            string outcomeFingerprint)
        {
            ProcessedOutcomeRecord prior;
            if (!_processedOutcomes.TryGetValue(outcomeCorrelationId, out prior))
            {
                return null;
            }

            OverworldRoomOutcomeApplyResult result;
            if (prior.Kind != outcomeKind || prior.Fingerprint != outcomeFingerprint)
            {
                result = OutcomeFailed(OverworldApplicationFailure.OutcomeConflict);
            }
            else
            {
                result = prior.AsReplay(CreatePersistenceSnapshot());
            }

            RecordSequence(sequence, sequenceFingerprint);
            _outcomeResults.Add(sequence, result);
            return result;
        }

        private void RecordProcessedOutcome(
            string kind,
            string correlationId,
            string fingerprint)
        {
            _processedOutcomes.Add(
                correlationId,
                new ProcessedOutcomeRecord(kind, correlationId, fingerprint));
        }

        private OverworldRoomOutcomeApplyResult OutcomeSucceeded(
            long sequence,
            MapNodeId roomId,
            IReadOnlyList<OverworldTransactionStepKind> steps,
            OverworldChapterAdvanceDecision decision,
            string outcomeCorrelationId)
        {
            _processedOutcomes[outcomeCorrelationId].SetCommit(roomId, steps, decision);
            var plan = new OverworldCommitPlan(
                sequence,
                roomId,
                steps,
                decision,
                CreatePersistenceSnapshot());
            var result = new OverworldRoomOutcomeApplyResult(
                OverworldApplicationFailure.None,
                OverworldOperationFailure.None,
                wasAlreadyApplied: false,
                plan);
            return result;
        }

        private static IReadOnlyList<OverworldTransactionStepKind> VictorySteps(
            bool chapterAdvanced)
        {
            var steps = new List<OverworldTransactionStepKind>
            {
                OverworldTransactionStepKind.AcceptReward,
                OverworldTransactionStepKind.ResolveRoom,
                OverworldTransactionStepKind.UnlockSuccessors
            };
            if (chapterAdvanced)
            {
                steps.Add(OverworldTransactionStepKind.AdvanceChapter);
            }

            steps.Add(OverworldTransactionStepKind.CommitPersistence);
            return new ReadOnlyCollection<OverworldTransactionStepKind>(steps);
        }

        private OverworldChapterAdvanceDecision CreateChapterDecision(bool chapterAdvanced)
        {
            if (!chapterAdvanced)
            {
                return new OverworldChapterAdvanceDecision(
                    OverworldChapterAdvanceKind.None,
                    _chapter,
                    nextChapter: 0);
            }

            return _chapter >= _finalChapter
                ? new OverworldChapterAdvanceDecision(
                    OverworldChapterAdvanceKind.FinalChapter,
                    _chapter,
                    nextChapter: 0)
                : new OverworldChapterAdvanceDecision(
                    OverworldChapterAdvanceKind.NextChapter,
                    _chapter,
                    _chapter + 1);
        }

        private SequenceCheck TryGetCombatLaunchReplay(
            long sequence,
            string fingerprint,
            out OverworldCombatLaunchResult result)
        {
            result = null;
            string prior;
            if (!_sequenceFingerprints.TryGetValue(sequence, out prior))
            {
                return SequenceCheck.New;
            }

            if (prior != fingerprint || !_combatLaunchResults.TryGetValue(sequence, out result))
            {
                return SequenceCheck.Conflict;
            }

            result = result.AsReplay();
            return SequenceCheck.Replay;
        }

        private SequenceCheck TryGetRoomEntryReplay(
            long sequence,
            string fingerprint,
            out OverworldRoomEntryResult result)
        {
            result = null;
            string prior;
            if (!_sequenceFingerprints.TryGetValue(sequence, out prior))
            {
                return SequenceCheck.New;
            }

            if (prior != fingerprint || !_roomEntryResults.TryGetValue(sequence, out result))
            {
                return SequenceCheck.Conflict;
            }

            result = result.AsReplay();
            return SequenceCheck.Replay;
        }

        private SequenceCheck TryGetOutcomeReplay(
            long sequence,
            string fingerprint,
            out OverworldRoomOutcomeApplyResult result)
        {
            result = null;
            string prior;
            if (!_sequenceFingerprints.TryGetValue(sequence, out prior))
            {
                return SequenceCheck.New;
            }

            if (prior != fingerprint || !_outcomeResults.TryGetValue(sequence, out result))
            {
                return SequenceCheck.Conflict;
            }

            result = result.AsReplay();
            return SequenceCheck.Replay;
        }

        private void MarkCommitted(long sequence)
        {
            if (sequence > _operationSequenceCursor)
            {
                _operationSequenceCursor = sequence;
            }

            _persistenceRevision++;
        }

        private void RecordSequence(long sequence, string fingerprint)
        {
            _sequenceFingerprints.Add(sequence, fingerprint);
        }

        private OverworldCombatLaunchResult CombatLaunchFailed(
            OverworldApplicationFailure failure,
            OverworldOperationFailure domainFailure = OverworldOperationFailure.None)
        {
            return new OverworldCombatLaunchResult(
                failure,
                domainFailure,
                wasAlreadyApplied: false,
                launch: null,
                CreatePersistenceSnapshot());
        }

        private OverworldRoomEntryResult RoomEntryFailed(
            OverworldApplicationFailure failure,
            OverworldOperationFailure domainFailure = OverworldOperationFailure.None)
        {
            return new OverworldRoomEntryResult(
                failure,
                domainFailure,
                wasAlreadyApplied: false,
                default(MapNodeId),
                default(OverworldRoomType),
                CreatePersistenceSnapshot());
        }

        private static OverworldRoomOutcomeApplyResult OutcomeFailed(
            OverworldApplicationFailure failure,
            OverworldOperationFailure domainFailure = OverworldOperationFailure.None)
        {
            return new OverworldRoomOutcomeApplyResult(
                failure,
                domainFailure,
                wasAlreadyApplied: false,
                commitPlan: null);
        }

        private static bool IsCombatRoom(OverworldRoomType roomType)
        {
            return roomType == OverworldRoomType.Battle ||
                   roomType == OverworldRoomType.Elite ||
                   roomType == OverworldRoomType.Boss;
        }

        private static OverworldApplicationFailure MapFailure(
            OverworldOperationFailure failure)
        {
            switch (failure)
            {
                case OverworldOperationFailure.InvalidCommand:
                    return OverworldApplicationFailure.InvalidCommand;
                case OverworldOperationFailure.InvalidSequence:
                    return OverworldApplicationFailure.InvalidSequence;
                case OverworldOperationFailure.SequenceConflict:
                    return OverworldApplicationFailure.SequenceConflict;
                case OverworldOperationFailure.StaleRevision:
                    return OverworldApplicationFailure.StaleRevision;
                case OverworldOperationFailure.SourceMismatch:
                    return OverworldApplicationFailure.SourceMismatch;
                case OverworldOperationFailure.MissingNode:
                    return OverworldApplicationFailure.MissingRoom;
                case OverworldOperationFailure.NotAdjacent:
                    return OverworldApplicationFailure.NotAdjacent;
                case OverworldOperationFailure.RoomAlreadySettled:
                    return OverworldApplicationFailure.RoomAlreadySettled;
                case OverworldOperationFailure.RoomInProgress:
                    return OverworldApplicationFailure.RoomInProgress;
                case OverworldOperationFailure.NoActiveRoom:
                    return OverworldApplicationFailure.NoActiveRoom;
                case OverworldOperationFailure.ActiveRoomMismatch:
                    return OverworldApplicationFailure.ActiveRoomMismatch;
                case OverworldOperationFailure.ChapterAlreadyAdvanced:
                    return OverworldApplicationFailure.ChapterAlreadyAdvanced;
                default:
                    return OverworldApplicationFailure.DomainRejected;
            }
        }

        private static OverworldMapProjection CreateMapProjection(
            OverworldMapDefinition map,
            OverworldMapGenerationConfig config)
        {
            var nodes = new List<OverworldMapNodeProjection>(map.Nodes.Count);
            var edges = new List<OverworldMapEdgeProjection>(map.Edges.Count);
            foreach (var node in map.Nodes)
            {
                nodes.Add(new OverworldMapNodeProjection(node));
            }

            foreach (var edge in map.Edges)
            {
                edges.Add(new OverworldMapEdgeProjection(edge.From, edge.To));
            }

            return new OverworldMapProjection(
                map.Seed,
                map.Chapter,
                map.EntryNodeId,
                map.BossNodeId,
                MapFingerprint(map, config),
                nodes,
                edges);
        }

        private static string MapFingerprint(
            OverworldMapDefinition map,
            OverworldMapGenerationConfig config)
        {
            var builder = new StringBuilder();
            builder.Append("map:").Append(map.Seed).Append(':').Append(map.Chapter)
                .Append(':').Append(config.IntermediateLayerCount)
                .Append(':').Append(config.MinimumNodesPerLayer)
                .Append(':').Append(config.MaximumNodesPerLayer).Append('|');
            foreach (var node in map.Nodes)
            {
                builder.Append(node.Id).Append(':').Append((int)node.RoomType).Append('|');
            }

            foreach (var edge in map.Edges)
            {
                builder.Append(edge.From).Append('>').Append(edge.To).Append('|');
            }

            return builder.ToString();
        }

        private static ReadOnlyCollection<string> Copy(IReadOnlyList<string> source)
        {
            return new ReadOnlyCollection<string>(new List<string>(source));
        }

        private static ReadOnlyCollection<string> Append(
            IReadOnlyList<string> source,
            string value)
        {
            var copy = new List<string>(source.Count + 1);
            copy.AddRange(source);
            copy.Add(value);
            return new ReadOnlyCollection<string>(copy);
        }

        private static IReadOnlyList<string> StableIds(IEnumerable<MapNodeId> source)
        {
            var result = new List<string>();
            foreach (var nodeId in source)
            {
                result.Add(nodeId.Value);
            }

            result.Sort(StringComparer.Ordinal);
            return new ReadOnlyCollection<string>(result);
        }

        private static MapNodeId OptionalNodeId(string value)
        {
            return string.IsNullOrEmpty(value) ? default(MapNodeId) : new MapNodeId(value);
        }

        private enum SequenceCheck
        {
            New,
            Replay,
            Conflict
        }

        private sealed class ProcessedOutcomeRecord
        {
            public ProcessedOutcomeRecord(
                string kind,
                string correlationId,
                string fingerprint)
                : this(
                    kind,
                    correlationId,
                    fingerprint,
                    default(MapNodeId),
                    Array.Empty<OverworldTransactionStepKind>(),
                    new OverworldChapterAdvanceDecision(
                        OverworldChapterAdvanceKind.None,
                        completedChapter: 0,
                        nextChapter: 0))
            {
            }

            public ProcessedOutcomeRecord(
                string kind,
                string correlationId,
                string fingerprint,
                MapNodeId roomId,
                IReadOnlyList<OverworldTransactionStepKind> steps,
                OverworldChapterAdvanceDecision decision)
            {
                Kind = kind;
                CorrelationId = correlationId;
                Fingerprint = fingerprint;
                RoomId = roomId;
                Steps = new ReadOnlyCollection<OverworldTransactionStepKind>(
                    new List<OverworldTransactionStepKind>(steps));
                Decision = decision;
            }

            public string Kind { get; }
            public string CorrelationId { get; }
            public string Fingerprint { get; }
            public MapNodeId RoomId { get; private set; }
            public IReadOnlyList<OverworldTransactionStepKind> Steps { get; private set; }
            public OverworldChapterAdvanceDecision Decision { get; private set; }

            public void SetCommit(
                MapNodeId roomId,
                IReadOnlyList<OverworldTransactionStepKind> steps,
                OverworldChapterAdvanceDecision decision)
            {
                RoomId = roomId;
                Steps = new ReadOnlyCollection<OverworldTransactionStepKind>(
                    new List<OverworldTransactionStepKind>(steps));
                Decision = decision;
            }

            public OverworldRoomOutcomeApplyResult AsReplay(
                OverworldPersistenceSnapshot snapshot)
            {
                var plan = new OverworldCommitPlan(
                    snapshot.OperationSequenceCursor,
                    RoomId,
                    Steps,
                    Decision,
                    snapshot);
                return new OverworldRoomOutcomeApplyResult(
                    OverworldApplicationFailure.None,
                    OverworldOperationFailure.None,
                    wasAlreadyApplied: true,
                    plan);
            }

            public ProcessedOutcomeRecord Copy()
            {
                return new ProcessedOutcomeRecord(
                    Kind,
                    CorrelationId,
                    Fingerprint,
                    RoomId,
                    Steps,
                    new OverworldChapterAdvanceDecision(
                        Decision.Kind,
                        Decision.CompletedChapter,
                        Decision.NextChapter));
            }
        }
    }
}
