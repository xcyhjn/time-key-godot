using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain.BattleFlow;

namespace TimeKey.Application.SceneFlow
{
    public enum CombatOutcomeApplyFailure
    {
        None,
        InvalidOutcome,
        RunMismatch,
        RoomMismatch,
        LaunchMismatch,
        OutcomeConflict,
        RoomAlreadySettled
    }

    public enum CombatLaunchApplyFailure
    {
        None,
        InvalidLaunch,
        RunMismatch,
        RoomAlreadySettled,
        StateMismatch
    }

    public sealed class CombatOutcomeApplyResult
    {
        internal CombatOutcomeApplyResult(
            CombatOutcomeApplyFailure failure,
            bool wasAlreadyApplied)
        {
            Failure = failure;
            WasAlreadyApplied = wasAlreadyApplied;
        }

        public bool Succeeded => Failure == CombatOutcomeApplyFailure.None;

        public CombatOutcomeApplyFailure Failure { get; }

        public bool WasAlreadyApplied { get; }
    }

    public sealed class OutOfBattleShellState
    {
        private readonly Dictionary<string, string> _consumedOutcomeFingerprints =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _settledRoomIds =
            new HashSet<string>(StringComparer.Ordinal);
        private ReadOnlyCollection<string> _deckStableIds;

        public OutOfBattleShellState(CombatLaunchPayload launch)
        {
            if (launch == null)
            {
                throw new ArgumentNullException(nameof(launch));
            }

            RunId = launch.RunId;
            RunSeed = launch.RunSeed;
            Chapter = launch.Chapter;
            CharacterId = launch.CharacterId;
            CurrentRoomId = launch.RoomId;
            CurrentLaunchCorrelationId = launch.LaunchCorrelationId;
            Era = launch.Era;
            Phase = launch.Phase;
            Timecoins = launch.Timecoins;
            _deckStableIds = Copy(launch.DeckStableIds);
        }

        public OutOfBattleShellState(RunStartPayload start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            RunId = start.RunId;
            RunSeed = start.RunSeed;
            Chapter = start.Chapter;
            CharacterId = start.CharacterId;
            CurrentRoomId = string.Empty;
            CurrentLaunchCorrelationId = string.Empty;
            Era = start.Era;
            Phase = start.Phase;
            Timecoins = start.Timecoins;
            _deckStableIds = Copy(start.DeckStableIds);
        }

        private OutOfBattleShellState(OutOfBattleShellState source)
        {
            RunId = source.RunId;
            RunSeed = source.RunSeed;
            Chapter = source.Chapter;
            CharacterId = source.CharacterId;
            CurrentRoomId = source.CurrentRoomId;
            CurrentLaunchCorrelationId = source.CurrentLaunchCorrelationId;
            Era = source.Era;
            Phase = source.Phase;
            Timecoins = source.Timecoins;
            _deckStableIds = Copy(source._deckStableIds);
            foreach (var pair in source._consumedOutcomeFingerprints)
            {
                _consumedOutcomeFingerprints.Add(pair.Key, pair.Value);
            }

            foreach (var roomId in source._settledRoomIds)
            {
                _settledRoomIds.Add(roomId);
            }
        }

        public string RunId { get; }

        public int RunSeed { get; }

        public int Chapter { get; }

        public string CharacterId { get; }

        public string CurrentRoomId { get; private set; }

        public string CurrentLaunchCorrelationId { get; private set; }

        public int Era { get; private set; }

        public int Phase { get; private set; }

        public int Timecoins { get; private set; }

        public IReadOnlyList<string> DeckStableIds => _deckStableIds;

        public IReadOnlyCollection<string> SettledRoomIds =>
            new ReadOnlyCollection<string>(new List<string>(_settledRoomIds));

        public bool IsRoomSettled(string roomId)
        {
            return !string.IsNullOrWhiteSpace(roomId) && _settledRoomIds.Contains(roomId);
        }

        public void SetCurrentRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("A current room identity is required.", nameof(roomId));
            }

            CurrentRoomId = roomId;
        }

        public CombatLaunchPayload CreateCombatLaunch(
            string launchCorrelationId,
            string roomId,
            string battleTag,
            int battleSeed)
        {
            if (IsRoomSettled(roomId))
            {
                throw new InvalidOperationException("A settled room cannot launch combat again.");
            }

            return new CombatLaunchPayload(
                launchCorrelationId,
                RunId,
                RunSeed,
                Chapter,
                Era,
                Phase,
                roomId,
                CharacterId,
                Timecoins,
                battleTag,
                battleSeed,
                _deckStableIds);
        }

        public OutOfBattleShellState Copy()
        {
            return new OutOfBattleShellState(this);
        }

        public CombatLaunchApplyFailure TryBeginCombat(CombatLaunchPayload launch)
        {
            if (launch == null)
            {
                return CombatLaunchApplyFailure.InvalidLaunch;
            }

            if (launch.RunId != RunId)
            {
                return CombatLaunchApplyFailure.RunMismatch;
            }

            if (launch.RunSeed != RunSeed || launch.Chapter != Chapter)
            {
                return CombatLaunchApplyFailure.StateMismatch;
            }

            if (_settledRoomIds.Contains(launch.RoomId))
            {
                return CombatLaunchApplyFailure.RoomAlreadySettled;
            }

            if (launch.Era != Era || launch.Phase != Phase ||
                launch.Timecoins != Timecoins || !DeckMatches(launch.DeckStableIds))
            {
                return CombatLaunchApplyFailure.StateMismatch;
            }

            CurrentRoomId = launch.RoomId;
            CurrentLaunchCorrelationId = launch.LaunchCorrelationId;
            return CombatLaunchApplyFailure.None;
        }

        public CombatOutcomeApplyResult TryApplyOutcome(CombatOutcome outcome)
        {
            if (outcome == null)
            {
                return Failed(CombatOutcomeApplyFailure.InvalidOutcome);
            }

            if (outcome.RunId != RunId)
            {
                return Failed(CombatOutcomeApplyFailure.RunMismatch);
            }

            if (outcome.RoomId != CurrentRoomId)
            {
                return Failed(CombatOutcomeApplyFailure.RoomMismatch);
            }

            if (outcome.LaunchCorrelationId != CurrentLaunchCorrelationId)
            {
                return Failed(CombatOutcomeApplyFailure.LaunchMismatch);
            }

            var fingerprint = Fingerprint(outcome);
            if (_consumedOutcomeFingerprints.TryGetValue(
                    outcome.OutcomeCorrelationId,
                    out var priorFingerprint))
            {
                return priorFingerprint == fingerprint
                    ? new CombatOutcomeApplyResult(CombatOutcomeApplyFailure.None, true)
                    : Failed(CombatOutcomeApplyFailure.OutcomeConflict);
            }

            if (outcome.ReturnPayload.Outcome == BattleOutcome.VictorySettlement &&
                _settledRoomIds.Contains(outcome.RoomId))
            {
                return Failed(CombatOutcomeApplyFailure.RoomAlreadySettled);
            }

            _consumedOutcomeFingerprints.Add(outcome.OutcomeCorrelationId, fingerprint);
            CurrentRoomId = outcome.RoomId;
            Era = outcome.ReturnPayload.Era;
            Phase = outcome.ReturnPayload.Phase;
            Timecoins = outcome.ReturnPayload.Timecoins;
            _deckStableIds = Copy(outcome.ReturnPayload.DeckStableIds);
            if (outcome.ReturnPayload.Outcome == BattleOutcome.VictorySettlement)
            {
                _settledRoomIds.Add(outcome.RoomId);
            }

            return new CombatOutcomeApplyResult(CombatOutcomeApplyFailure.None, false);
        }

        private static string Fingerprint(CombatOutcome outcome)
        {
            var payload = outcome.ReturnPayload;
            return outcome.LaunchCorrelationId + "|" + outcome.RunId + "|" + outcome.RoomId +
                "|" + payload.Outcome + "|" + payload.Era + "|" + payload.Phase + "|" +
                payload.Timecoins + "|" + payload.BattleTag + "|" + payload.BattleSeed + "|" +
                string.Join(",", payload.DeckStableIds);
        }

        private static ReadOnlyCollection<string> Copy(IReadOnlyList<string> source)
        {
            return new ReadOnlyCollection<string>(new List<string>(source));
        }

        private bool DeckMatches(IReadOnlyList<string> deckStableIds)
        {
            if (deckStableIds == null || deckStableIds.Count != _deckStableIds.Count)
            {
                return false;
            }

            for (var index = 0; index < deckStableIds.Count; index++)
            {
                if (deckStableIds[index] != _deckStableIds[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static CombatOutcomeApplyResult Failed(CombatOutcomeApplyFailure failure)
        {
            return new CombatOutcomeApplyResult(failure, false);
        }
    }
}
