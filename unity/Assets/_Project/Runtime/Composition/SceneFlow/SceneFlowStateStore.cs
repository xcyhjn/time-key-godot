using System;
using System.IO;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Infrastructure.Persistence;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    public enum OverworldLocalRoomCommitFailure
    {
        None,
        InvalidRequest,
        EntryRejected,
        OutcomeRejected,
        PersistenceFailed
    }

    public sealed class OverworldLocalRoomCommitResult
    {
        internal OverworldLocalRoomCommitResult(
            OverworldLocalRoomCommitFailure failure,
            OverworldApplicationFailure applicationFailure,
            OverworldPersistenceSnapshot snapshot,
            OverworldShopOffer shopOffer,
            string detail)
        {
            Failure = failure;
            ApplicationFailure = applicationFailure;
            Snapshot = snapshot;
            ShopOffer = shopOffer;
            Detail = detail ?? string.Empty;
        }

        public bool Succeeded => Failure == OverworldLocalRoomCommitFailure.None;
        public OverworldLocalRoomCommitFailure Failure { get; }
        public OverworldApplicationFailure ApplicationFailure { get; }
        public OverworldPersistenceSnapshot Snapshot { get; }
        public OverworldShopOffer ShopOffer { get; }
        public string Detail { get; }
    }

    [DisallowMultipleComponent]
    public sealed class SceneFlowStateStore : MonoBehaviour
    {
        [SerializeField] private int finalChapter = 3;
        [SerializeField] private int intermediateLayerCount = 4;
        [SerializeField] private int minimumNodesPerLayer = 2;
        [SerializeField] private int maximumNodesPerLayer = 3;
        [SerializeField] private string saveFileName = "overworld-save.json";

        private PendingRecord _pending;
        private PreparedLaunchRecord _preparedLaunch;
        private OverworldRunApplication _preparedContinue;
        private OverworldSaveRepository _repository;

        public SceneTransitionRequest LastRequest { get; private set; }

        public ISceneTransitionPayload LastPayload { get; private set; }

        public CombatLaunchPayload ActiveLaunch { get; private set; }

        public CombatOutcome LastOutcome { get; private set; }

        public OutOfBattleShellState OutOfBattleState { get; private set; }

        public CombatOutcomeApplyResult LastOutcomeApplyResult { get; private set; }

        public OverworldRunApplication OverworldRun { get; private set; }

        public OverworldSaveLoadStatus ContinueStatus { get; private set; } =
            OverworldSaveLoadStatus.Missing;

        public string ContinueDetail { get; private set; } = string.Empty;

        public void ConfigurePersistencePath(string path)
        {
            if (_pending != null)
            {
                throw new InvalidOperationException(
                    "Persistence cannot be reconfigured during a scene transition.");
            }

            _repository = new OverworldSaveRepository(path);
            _preparedContinue = null;
        }

        public void ConfigurePersistenceRepository(OverworldSaveRepository repository)
        {
            if (_pending != null)
            {
                throw new InvalidOperationException(
                    "Persistence cannot be reconfigured during a scene transition.");
            }

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _preparedContinue = null;
        }

        public bool RefreshContinueAvailability()
        {
            var load = Repository.Load();
            ContinueStatus = load.Status;
            ContinueDetail = load.Detail;
            _preparedContinue = null;
            if (!load.Succeeded)
            {
                return false;
            }

            try
            {
                _preparedContinue = OverworldRunApplication.Restore(
                    OverworldSaveMapper.ToSnapshot(load.Document));
                return true;
            }
            catch (Exception exception)
            {
                ContinueStatus = OverworldSaveLoadStatus.Corrupt;
                ContinueDetail = exception.Message;
                return false;
            }
        }

        public bool TryCreateContinuePayload(out RunStartPayload payload)
        {
            payload = null;
            if (_preparedContinue == null && !RefreshContinueAvailability())
            {
                return false;
            }

            var snapshot = _preparedContinue.CreatePersistenceSnapshot();
            payload = new RunStartPayload(
                RunStartKind.Continue,
                snapshot.RunId,
                snapshot.RunSeed,
                snapshot.SeedText,
                snapshot.Chapter,
                snapshot.Era,
                snapshot.Phase,
                snapshot.Timecoins,
                snapshot.CharacterId,
                snapshot.DeckStableIds);
            return true;
        }

        public OverworldLocalRoomCommitResult CompleteEventRoom(
            long entrySequence,
            long outcomeSequence,
            string roomId)
        {
            if (!CanCommitLocalRoom(entrySequence, outcomeSequence, roomId))
            {
                return LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.InvalidRequest,
                    OverworldApplicationFailure.InvalidCommand,
                    "A valid event room request is required.");
            }

            var candidate = OverworldRun.Copy();
            var target = new MapNodeId(roomId);
            var entry = candidate.TryEnterEventRoom(new EnterOverworldRoomCommand(
                entrySequence,
                candidate.Revision,
                candidate.ChapterSnapshot.CurrentNodeId,
                target));
            if (!entry.Succeeded)
            {
                return LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.EntryRejected,
                    entry.Failure,
                    "Event room entry was rejected.");
            }

            var outcome = candidate.TryApplyEventOutcome(
                outcomeSequence,
                candidate.Revision,
                new EventRoomOutcome(
                    "event-" + outcomeSequence,
                    candidate.CreatePersistenceSnapshot().RunId,
                    target,
                    EventRoomCompletion.Completed));
            return outcome.Succeeded
                ? PromoteLocalRoom(candidate, outcome.CommitPlan.PersistenceSnapshot, null)
                : LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.OutcomeRejected,
                    outcome.Failure,
                    "Event room outcome was rejected.");
        }

        public OverworldLocalRoomCommitResult PurchaseShopRoom(
            long entrySequence,
            long outcomeSequence,
            string roomId)
        {
            if (!CanCommitLocalRoom(entrySequence, outcomeSequence, roomId))
            {
                return LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.InvalidRequest,
                    OverworldApplicationFailure.InvalidCommand,
                    "A valid shop room request is required.");
            }

            var candidate = OverworldRun.Copy();
            var target = new MapNodeId(roomId);
            OverworldShopOffer offer;
            try
            {
                offer = candidate.GetShopOffer(target);
            }
            catch (InvalidOperationException exception)
            {
                return LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.InvalidRequest,
                    OverworldApplicationFailure.InvalidRoomType,
                    exception.Message);
            }

            var entry = candidate.TryEnterShopRoom(new EnterOverworldRoomCommand(
                entrySequence,
                candidate.Revision,
                candidate.ChapterSnapshot.CurrentNodeId,
                target));
            if (!entry.Succeeded)
            {
                return LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.EntryRejected,
                    entry.Failure,
                    "Shop room entry was rejected.");
            }

            var outcome = candidate.TryApplyShopOutcome(
                outcomeSequence,
                candidate.Revision,
                new ShopRoomOutcome(
                    "shop-" + outcomeSequence,
                    candidate.CreatePersistenceSnapshot().RunId,
                    target,
                    ShopRoomCompletion.Completed,
                    offer.CardStableId,
                    offer.TimecoinCost));
            return outcome.Succeeded
                ? PromoteLocalRoom(candidate, outcome.CommitPlan.PersistenceSnapshot, offer)
                : LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.OutcomeRejected,
                    outcome.Failure,
                    "Shop room outcome was rejected.");
        }

        public OverworldCombatLaunchResult PrepareCombatLaunch(
            long sequence,
            string roomId,
            string launchCorrelationId,
            string battleTag,
            int battleSeed)
        {
            if (OverworldRun == null)
            {
                throw new InvalidOperationException("No authoritative overworld run is active.");
            }

            var candidate = OverworldRun.Copy();
            var current = candidate.ChapterSnapshot.CurrentNodeId;
            var result = candidate.TryBeginCombat(
                new EnterOverworldRoomCommand(
                    sequence,
                    candidate.Revision,
                    current,
                    new MapNodeId(roomId)),
                launchCorrelationId,
                battleTag,
                battleSeed);
            if (result.Succeeded)
            {
                _preparedLaunch = new PreparedLaunchRecord(result.Launch.Fingerprint, candidate);
            }

            return result;
        }

        public void Record(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (_pending != null)
            {
                throw new InvalidOperationException(
                    "The prior scene-flow state record is still pending.");
            }

            var nextOutOfBattleState = OutOfBattleState?.Copy();
            var nextOverworldRun = OverworldRun;
            var nextActiveLaunch = ActiveLaunch;
            var nextLastOutcome = LastOutcome;
            var nextOutcomeApplyResult = LastOutcomeApplyResult;

            if (request.Payload is CombatLaunchPayload launch)
            {
                if (_preparedLaunch != null &&
                    _preparedLaunch.PayloadFingerprint == launch.Fingerprint)
                {
                    nextOverworldRun = _preparedLaunch.Application;
                    nextOutOfBattleState = new OutOfBattleShellState(
                        nextOverworldRun.CreatePersistenceSnapshot());
                    nextActiveLaunch = launch;
                    _preparedLaunch = null;
                }
                else
                {
                    RecordLaunch(launch, ref nextOutOfBattleState, out nextActiveLaunch);
                }
            }
            else if (request.Payload is RunStartPayload start)
            {
                nextOverworldRun = start.Kind == RunStartKind.Continue
                    ? RequirePreparedContinue(start)
                    : new OverworldRunApplication(
                        start,
                        CreateMapConfig(start.Chapter),
                        finalChapter);
                nextOutOfBattleState = new OutOfBattleShellState(
                    nextOverworldRun.CreatePersistenceSnapshot());
                nextActiveLaunch = null;
                nextLastOutcome = null;
                nextOutcomeApplyResult = null;
            }
            else if (request.Payload is CombatOutcome outcome)
            {
                RecordOutcome(
                    outcome,
                    nextActiveLaunch,
                    nextLastOutcome,
                    nextOutOfBattleState,
                    out nextLastOutcome,
                    out nextOutcomeApplyResult);
                if (nextOverworldRun != null &&
                    nextOverworldRun.ChapterSnapshot.HasActiveRoom &&
                    nextOverworldRun.ChapterSnapshot.ActiveRoomNodeId.Value == outcome.RoomId)
                {
                    var candidate = nextOverworldRun.Copy();
                    var applicationResult = candidate.TryApplyCombatOutcome(
                        request.Sequence,
                        candidate.Revision,
                        outcome);
                    if (!applicationResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Combat outcome could not be applied to the overworld: " +
                            applicationResult.Failure + ".");
                    }

                    if (applicationResult.CommitPlan.ChapterDecision.Kind ==
                        OverworldChapterAdvanceKind.NextChapter)
                    {
                        candidate = candidate.CreateNextChapter(
                            applicationResult.CommitPlan.ChapterDecision.NextChapter);
                        nextOutOfBattleState = new OutOfBattleShellState(
                            candidate.CreatePersistenceSnapshot());
                    }

                    nextOverworldRun = candidate;
                }

                nextActiveLaunch = null;
            }
            else if (request.Payload is EmptySceneTransitionPayload &&
                     request.Source == SceneId.GameOver &&
                     request.Target == SceneId.MainMenu)
            {
                nextOutOfBattleState = null;
                nextActiveLaunch = null;
                nextLastOutcome = null;
                nextOutcomeApplyResult = null;
                nextOverworldRun = null;
            }

            _pending = new PendingRecord(
                request.Fingerprint,
                LastRequest,
                LastPayload,
                ActiveLaunch,
                LastOutcome,
                OutOfBattleState,
                LastOutcomeApplyResult,
                OverworldRun);
            LastRequest = request;
            LastPayload = request.Payload;
            ActiveLaunch = nextActiveLaunch;
            LastOutcome = nextLastOutcome;
            OutOfBattleState = nextOutOfBattleState;
            LastOutcomeApplyResult = nextOutcomeApplyResult;
            OverworldRun = nextOverworldRun;
        }

        public void PrepareCommit(SceneTransitionRequest request)
        {
            RequirePending(request);
            if (_pending.PersistencePromoted)
            {
                return;
            }

            _pending.PriorSave = Repository.Load();
            OverworldSaveWriteResult write;
            if (request.Payload is EmptySceneTransitionPayload &&
                request.Source == SceneId.GameOver &&
                request.Target == SceneId.MainMenu)
            {
                write = Repository.DeleteAll();
            }
            else if (ShouldPersist(request))
            {
                write = Repository.Save(OverworldSaveMapper.ToDocument(
                    OverworldRun.CreatePersistenceSnapshot()));
            }
            else
            {
                return;
            }

            if (!write.Succeeded)
            {
                throw new IOException(
                    "Overworld persistence prepare failed: " + write.Failure +
                    ": " + write.Detail);
            }

            _pending.PersistencePromoted = true;
        }

        public void Commit(SceneTransitionRequest request)
        {
            RequirePending(request);
            _pending = null;
            _preparedContinue = null;
        }

        public void Rollback(SceneTransitionRequest request)
        {
            if (_pending == null)
            {
                _preparedLaunch = null;
                return;
            }

            RequirePending(request);
            LastRequest = _pending.LastRequest;
            LastPayload = _pending.LastPayload;
            ActiveLaunch = _pending.ActiveLaunch;
            LastOutcome = _pending.LastOutcome;
            OutOfBattleState = _pending.OutOfBattleState;
            LastOutcomeApplyResult = _pending.LastOutcomeApplyResult;
            OverworldRun = _pending.OverworldRun;
            RestorePriorSave(_pending);
            _pending = null;
            _preparedLaunch = null;
        }

        private OverworldRunApplication RequirePreparedContinue(RunStartPayload start)
        {
            if (_preparedContinue == null || _preparedContinue.CreatePersistenceSnapshot().RunId !=
                start.RunId)
            {
                throw new InvalidOperationException(
                    "Continue payload does not match the validated save.");
            }

            return _preparedContinue.Copy();
        }

        private bool CanCommitLocalRoom(
            long entrySequence,
            long outcomeSequence,
            string roomId)
        {
            return OverworldRun != null && _pending == null &&
                entrySequence > 0 && outcomeSequence > entrySequence &&
                !string.IsNullOrWhiteSpace(roomId);
        }

        private OverworldLocalRoomCommitResult PromoteLocalRoom(
            OverworldRunApplication candidate,
            OverworldPersistenceSnapshot snapshot,
            OverworldShopOffer shopOffer)
        {
            var write = Repository.Save(OverworldSaveMapper.ToDocument(snapshot));
            if (!write.Succeeded)
            {
                return LocalRoomFailed(
                    OverworldLocalRoomCommitFailure.PersistenceFailed,
                    OverworldApplicationFailure.None,
                    write.Failure + ": " + write.Detail);
            }

            OverworldRun = candidate;
            OutOfBattleState = new OutOfBattleShellState(snapshot);
            _preparedContinue = null;
            return new OverworldLocalRoomCommitResult(
                OverworldLocalRoomCommitFailure.None,
                OverworldApplicationFailure.None,
                snapshot,
                shopOffer,
                string.Empty);
        }

        private static OverworldLocalRoomCommitResult LocalRoomFailed(
            OverworldLocalRoomCommitFailure failure,
            OverworldApplicationFailure applicationFailure,
            string detail)
        {
            return new OverworldLocalRoomCommitResult(
                failure,
                applicationFailure,
                null,
                null,
                detail);
        }

        private OverworldMapGenerationConfig CreateMapConfig(int chapter)
        {
            return new OverworldMapGenerationConfig(
                chapter,
                intermediateLayerCount,
                minimumNodesPerLayer,
                maximumNodesPerLayer);
        }

        private bool ShouldPersist(SceneTransitionRequest request)
        {
            if (OverworldRun == null)
            {
                return false;
            }

            if (request.Payload is RunStartPayload)
            {
                return true;
            }

            return request.Payload is CombatOutcome outcome &&
                outcome.TargetScene == SceneId.OutOfBattleShell;
        }

        private void RestorePriorSave(PendingRecord pending)
        {
            if (!pending.PersistencePromoted)
            {
                return;
            }

            OverworldSaveWriteResult restore;
            if (pending.PriorSave != null && pending.PriorSave.Succeeded)
            {
                restore = Repository.Save(pending.PriorSave.Document);
            }
            else
            {
                restore = Repository.DeleteAll();
            }

            if (!restore.Succeeded)
            {
                throw new IOException(
                    "Overworld persistence rollback failed: " + restore.Failure +
                    ": " + restore.Detail);
            }
        }

        private OverworldSaveRepository Repository
        {
            get
            {
                if (_repository == null)
                {
                    _repository = new OverworldSaveRepository(
                        Path.Combine(UnityEngine.Application.persistentDataPath, saveFileName));
                }

                return _repository;
            }
        }

        private static void RecordLaunch(
            CombatLaunchPayload launch,
            ref OutOfBattleShellState outOfBattleState,
            out CombatLaunchPayload activeLaunch)
        {
            if (outOfBattleState == null)
            {
                outOfBattleState = new OutOfBattleShellState(launch);
            }
            else
            {
                var failure = outOfBattleState.TryBeginCombat(launch);
                if (failure != CombatLaunchApplyFailure.None)
                {
                    throw new InvalidOperationException(
                        "Combat launch could not be applied to the out-of-battle state: " +
                        failure + ".");
                }
            }

            activeLaunch = launch;
        }

        private static void RecordOutcome(
            CombatOutcome outcome,
            CombatLaunchPayload activeLaunch,
            CombatOutcome priorOutcome,
            OutOfBattleShellState outOfBattleState,
            out CombatOutcome lastOutcome,
            out CombatOutcomeApplyResult outcomeApplyResult)
        {
            var isExactReplay = activeLaunch == null && priorOutcome != null &&
                string.Equals(
                    priorOutcome.Fingerprint,
                    outcome.Fingerprint,
                    StringComparison.Ordinal);
            if (outOfBattleState == null ||
                (!isExactReplay &&
                 (activeLaunch == null ||
                  outcome.RunId != activeLaunch.RunId ||
                  outcome.RoomId != activeLaunch.RoomId ||
                  outcome.LaunchCorrelationId != activeLaunch.LaunchCorrelationId)))
            {
                throw new InvalidOperationException(
                    "Combat outcome does not match the active combat launch.");
            }

            outcomeApplyResult = outOfBattleState.TryApplyOutcome(outcome);
            if (!outcomeApplyResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Combat outcome could not be consumed: " +
                    outcomeApplyResult.Failure + ".");
            }

            lastOutcome = outcome;
        }

        private void RequirePending(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (_pending == null || _pending.RequestFingerprint != request.Fingerprint)
            {
                throw new InvalidOperationException(
                    "The scene-flow state record does not match the pending request.");
            }
        }

        private sealed class PendingRecord
        {
            public PendingRecord(
                string requestFingerprint,
                SceneTransitionRequest lastRequest,
                ISceneTransitionPayload lastPayload,
                CombatLaunchPayload activeLaunch,
                CombatOutcome lastOutcome,
                OutOfBattleShellState outOfBattleState,
                CombatOutcomeApplyResult lastOutcomeApplyResult,
                OverworldRunApplication overworldRun)
            {
                RequestFingerprint = requestFingerprint;
                LastRequest = lastRequest;
                LastPayload = lastPayload;
                ActiveLaunch = activeLaunch;
                LastOutcome = lastOutcome;
                OutOfBattleState = outOfBattleState;
                LastOutcomeApplyResult = lastOutcomeApplyResult;
                OverworldRun = overworldRun;
            }

            public string RequestFingerprint { get; }

            public SceneTransitionRequest LastRequest { get; }

            public ISceneTransitionPayload LastPayload { get; }

            public CombatLaunchPayload ActiveLaunch { get; }

            public CombatOutcome LastOutcome { get; }

            public OutOfBattleShellState OutOfBattleState { get; }

            public CombatOutcomeApplyResult LastOutcomeApplyResult { get; }

            public OverworldRunApplication OverworldRun { get; }

            public OverworldSaveLoadResult PriorSave { get; set; }

            public bool PersistencePromoted { get; set; }
        }

        private sealed class PreparedLaunchRecord
        {
            public PreparedLaunchRecord(
                string payloadFingerprint,
                OverworldRunApplication application)
            {
                PayloadFingerprint = payloadFingerprint;
                Application = application;
            }

            public string PayloadFingerprint { get; }

            public OverworldRunApplication Application { get; }
        }
    }
}
