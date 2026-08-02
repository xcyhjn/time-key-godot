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
        OutcomeConflict,
        RoomAlreadySettled
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
            CurrentRoomId = launch.RoomId;
            Era = launch.Era;
            Phase = launch.Phase;
            Timecoins = launch.Timecoins;
            _deckStableIds = Copy(launch.DeckStableIds);
        }

        public string RunId { get; }

        public string CurrentRoomId { get; private set; }

        public int Era { get; private set; }

        public int Phase { get; private set; }

        public int Timecoins { get; private set; }

        public IReadOnlyList<string> DeckStableIds => _deckStableIds;

        public IReadOnlyCollection<string> SettledRoomIds =>
            new ReadOnlyCollection<string>(new List<string>(_settledRoomIds));

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

        private static CombatOutcomeApplyResult Failed(CombatOutcomeApplyFailure failure)
        {
            return new CombatOutcomeApplyResult(failure, false);
        }
    }
}
