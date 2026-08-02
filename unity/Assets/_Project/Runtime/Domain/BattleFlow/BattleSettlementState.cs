using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain.BattleFlow
{
    public enum BattleOutcome
    {
        Active,
        VictorySettlement,
        Defeat
    }

    public enum BattleSettlementFailure
    {
        None,
        InvalidSequence,
        InvalidOutcome,
        SequenceConflict,
        OutcomeConflict,
        RewardUnavailable,
        RewardAlreadyClaimed
    }

    public enum BattleRewardKind
    {
        Shop,
        Acquire,
        Remove,
        Craft
    }

    public enum BattleReturnCompletion
    {
        VictoryCompleted,
        Defeat
    }

    public enum BattleReturnFailure
    {
        None,
        OutcomeNotResolved,
        InvalidRoundSnapshot,
        InvalidDeckSnapshot,
        InvalidDeckStableId
    }

    public sealed class BattleRewardEntry
    {
        public BattleRewardEntry(
            string entryStableId,
            BattleRewardKind rewardKind,
            string rewardLabel)
        {
            if (rewardKind != BattleRewardKind.Shop &&
                rewardKind != BattleRewardKind.Acquire &&
                rewardKind != BattleRewardKind.Remove &&
                rewardKind != BattleRewardKind.Craft)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardKind));
            }

            EntryStableId = Required(entryStableId, nameof(entryStableId));
            RewardKind = rewardKind;
            RewardLabel = Required(rewardLabel, nameof(rewardLabel));
        }

        public string EntryStableId { get; }

        public BattleRewardKind RewardKind { get; }

        public string RewardLabel { get; }

        private static string Required(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty value is required.", parameterName);
            }

            return value;
        }
    }

    public sealed class BattleSettlementSnapshot
    {
        internal BattleSettlementSnapshot(
            string battleTag,
            int battleSeed,
            BattleOutcome outcome,
            BattleRewardEntry rewardEntry,
            bool isRewardClaimed)
        {
            BattleTag = battleTag;
            BattleSeed = battleSeed;
            Outcome = outcome;
            RewardEntry = rewardEntry;
            IsRewardClaimed = isRewardClaimed;
        }

        public string BattleTag { get; }

        public int BattleSeed { get; }

        public BattleOutcome Outcome { get; }

        public bool IsInputLocked => Outcome != BattleOutcome.Active;

        public BattleRewardEntry RewardEntry { get; }

        public bool IsRewardClaimed { get; }
    }

    public sealed class BattleSettlementResult
    {
        internal BattleSettlementResult(
            long sequence,
            BattleSettlementFailure failure,
            BattleSettlementSnapshot snapshot)
        {
            Sequence = sequence;
            Failure = failure;
            Snapshot = snapshot;
        }

        public bool Succeeded => Failure == BattleSettlementFailure.None;

        public long Sequence { get; }

        public BattleSettlementFailure Failure { get; }

        public BattleSettlementSnapshot Snapshot { get; }
    }

    public sealed class BattleReturnPayload
    {
        private readonly ReadOnlyCollection<string> _deckStableIds;

        internal BattleReturnPayload(
            BattleOutcome outcome,
            BattleReturnCompletion completion,
            int era,
            int phase,
            int timecoins,
            IReadOnlyList<string> deckStableIds,
            string battleTag,
            int battleSeed)
        {
            Outcome = outcome;
            Completion = completion;
            Era = era;
            Phase = phase;
            Timecoins = timecoins;
            BattleTag = battleTag;
            BattleSeed = battleSeed;
            _deckStableIds = Copy(deckStableIds);
        }

        public BattleOutcome Outcome { get; }

        public BattleReturnCompletion Completion { get; }

        public bool IsCompleted => Completion == BattleReturnCompletion.VictoryCompleted;

        public int Era { get; }

        public int Phase { get; }

        public int Timecoins { get; }

        public IReadOnlyList<string> DeckStableIds => _deckStableIds;

        public string BattleTag { get; }

        public int BattleSeed { get; }

        private static ReadOnlyCollection<string> Copy(IReadOnlyList<string> source)
        {
            var copied = new List<string>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                copied.Add(source[index]);
            }

            return new ReadOnlyCollection<string>(copied);
        }
    }

    public sealed class BattleReturnResult
    {
        internal BattleReturnResult(
            BattleReturnFailure failure,
            BattleReturnPayload payload)
        {
            Failure = failure;
            Payload = payload;
        }

        public bool Succeeded => Failure == BattleReturnFailure.None;

        public BattleReturnFailure Failure { get; }

        public BattleReturnPayload Payload { get; }
    }

    public sealed class BattleSettlementState
    {
        private readonly string _battleTag;
        private readonly int _battleSeed;
        private readonly BattleRewardEntry _victoryRewardEntry;
        private readonly Dictionary<long, BattleOutcome> _resolutionRequests =
            new Dictionary<long, BattleOutcome>();
        private readonly Dictionary<long, BattleSettlementResult> _rewardClaims =
            new Dictionary<long, BattleSettlementResult>();

        private BattleSettlementSnapshot _snapshot;
        private BattleSettlementResult _resolutionResult;

        public BattleSettlementState(
            string battleTag,
            int battleSeed,
            BattleRewardEntry victoryRewardEntry)
        {
            if (string.IsNullOrWhiteSpace(battleTag))
            {
                throw new ArgumentException("A battle tag is required.", nameof(battleTag));
            }

            _battleTag = battleTag;
            _battleSeed = battleSeed;
            _victoryRewardEntry = victoryRewardEntry ??
                throw new ArgumentNullException(nameof(victoryRewardEntry));
            _snapshot = new BattleSettlementSnapshot(
                _battleTag,
                _battleSeed,
                BattleOutcome.Active,
                rewardEntry: null,
                isRewardClaimed: false);
        }

        public BattleSettlementSnapshot Snapshot => _snapshot;

        public BattleSettlementResult TryResolve(long sequence, BattleOutcome requestedOutcome)
        {
            if (sequence <= 0)
            {
                return Failed(sequence, BattleSettlementFailure.InvalidSequence);
            }

            if (requestedOutcome != BattleOutcome.VictorySettlement &&
                requestedOutcome != BattleOutcome.Defeat)
            {
                return Failed(sequence, BattleSettlementFailure.InvalidOutcome);
            }

            if (_resolutionRequests.TryGetValue(sequence, out var priorRequest))
            {
                return priorRequest == requestedOutcome
                    ? _resolutionResult
                    : Failed(sequence, BattleSettlementFailure.SequenceConflict);
            }

            if (_snapshot.Outcome != BattleOutcome.Active)
            {
                if (_snapshot.Outcome == requestedOutcome)
                {
                    return _resolutionResult;
                }

                return Failed(sequence, BattleSettlementFailure.OutcomeConflict);
            }

            _snapshot = new BattleSettlementSnapshot(
                _battleTag,
                _battleSeed,
                requestedOutcome,
                requestedOutcome == BattleOutcome.VictorySettlement
                    ? _victoryRewardEntry
                    : null,
                isRewardClaimed: false);
            _resolutionResult = new BattleSettlementResult(
                sequence,
                BattleSettlementFailure.None,
                _snapshot);
            _resolutionRequests.Add(sequence, requestedOutcome);
            return _resolutionResult;
        }

        public BattleSettlementResult TryClaimReward(long sequence)
        {
            if (sequence <= 0)
            {
                return Failed(sequence, BattleSettlementFailure.InvalidSequence);
            }

            if (_rewardClaims.TryGetValue(sequence, out var priorResult))
            {
                return priorResult;
            }

            BattleSettlementResult result;
            if (_snapshot.Outcome != BattleOutcome.VictorySettlement ||
                _snapshot.RewardEntry == null)
            {
                result = Failed(sequence, BattleSettlementFailure.RewardUnavailable);
            }
            else if (_snapshot.IsRewardClaimed)
            {
                result = Failed(sequence, BattleSettlementFailure.RewardAlreadyClaimed);
            }
            else
            {
                _snapshot = new BattleSettlementSnapshot(
                    _battleTag,
                    _battleSeed,
                    _snapshot.Outcome,
                    _snapshot.RewardEntry,
                    isRewardClaimed: true);
                result = new BattleSettlementResult(
                    sequence,
                    BattleSettlementFailure.None,
                    _snapshot);
            }

            _rewardClaims.Add(sequence, result);
            return result;
        }

        public BattleReturnResult TryCreateReturnBoundary(
            BattleRoundSnapshot round,
            IReadOnlyList<string> deckStableIds)
        {
            if (_snapshot.Outcome == BattleOutcome.Active)
            {
                return ReturnFailed(BattleReturnFailure.OutcomeNotResolved);
            }

            if (round == null)
            {
                return ReturnFailed(BattleReturnFailure.InvalidRoundSnapshot);
            }

            if (deckStableIds == null)
            {
                return ReturnFailed(BattleReturnFailure.InvalidDeckSnapshot);
            }

            for (var index = 0; index < deckStableIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(deckStableIds[index]))
                {
                    return ReturnFailed(BattleReturnFailure.InvalidDeckStableId);
                }
            }

            var completion = _snapshot.Outcome == BattleOutcome.VictorySettlement
                ? BattleReturnCompletion.VictoryCompleted
                : BattleReturnCompletion.Defeat;
            return new BattleReturnResult(
                BattleReturnFailure.None,
                new BattleReturnPayload(
                    _snapshot.Outcome,
                    completion,
                    round.Era,
                    round.Phase,
                    round.Timecoins,
                    deckStableIds,
                    _battleTag,
                    _battleSeed));
        }

        private BattleSettlementResult Failed(
            long sequence,
            BattleSettlementFailure failure)
        {
            return new BattleSettlementResult(sequence, failure, _snapshot);
        }

        private static BattleReturnResult ReturnFailed(BattleReturnFailure failure)
        {
            return new BattleReturnResult(failure, payload: null);
        }
    }
}
