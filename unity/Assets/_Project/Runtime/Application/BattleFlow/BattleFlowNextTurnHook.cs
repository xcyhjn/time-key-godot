using System;
using System.Collections.Generic;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;

namespace TimeKey.Application.BattleFlow
{
    public sealed class BattleFlowNextTurnHook : INextTurnHook
    {
        private readonly DeckState _deck;
        private readonly BattleRoundLedger _rounds;
        private readonly BattleSettlementState _settlement;
        private readonly Dictionary<long, BattleFlowHookRequest> _requests =
            new Dictionary<long, BattleFlowHookRequest>();
        private readonly Dictionary<long, BattleFlowHookResult> _results =
            new Dictionary<long, BattleFlowHookResult>();

        public BattleFlowNextTurnHook(
            DeckState deck,
            BattleRoundLedger rounds,
            BattleSettlementState settlement)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _rounds = rounds ?? throw new ArgumentNullException(nameof(rounds));
            _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
            Current = Capture(Array.Empty<TimelineActionPresentationSnapshot>());
        }

        public BattleFlowPresentationSnapshot Current { get; private set; }

        public BattleFlowHookResult LastResult { get; private set; }

        public DeckMoveResult DiscardPlayedCard(
            DeckCommandId commandId,
            CardInstanceId instanceId)
        {
            if (_settlement.Snapshot.IsInputLocked)
            {
                return null;
            }

            var result = _deck.DiscardFromHand(commandId, instanceId);
            if (result.Succeeded)
            {
                Current = Capture(Current.ActionDisplaySnapshots);
            }

            return result;
        }

        public BattleSettlementResult TryResolveOutcome(
            long sequence,
            BattleOutcome outcome)
        {
            var result = _settlement.TryResolve(sequence, outcome);
            if (result.Succeeded)
            {
                Current = Capture(Current.ActionDisplaySnapshots);
            }

            return result;
        }

        public BattleSettlementResult TryClaimReward(long sequence)
        {
            var result = _settlement.TryClaimReward(sequence);
            if (result.Succeeded)
            {
                Current = Capture(Current.ActionDisplaySnapshots);
            }

            return result;
        }

        public BattleReturnResult TryCreateReturnBoundary()
        {
            var deck = _deck.Snapshot();
            var stableIds = new List<string>(deck.Counts.Total);
            AddStableIds(stableIds, deck.DrawPile);
            AddStableIds(stableIds, deck.Hand);
            AddStableIds(stableIds, deck.DiscardPile);
            return _settlement.TryCreateReturnBoundary(_rounds.Snapshot, stableIds);
        }

        public BattleFlowHookResult PrepareInitialStart(long sequence)
        {
            return Prepare(BattleFlowHookRequest.InitialStart(sequence));
        }

        public BattleFlowHookResult PrepareEndTurn(
            long sequence,
            int occupiedCellCount,
            IReadOnlyList<CardInstanceId> remainingHandInstanceIds,
            IReadOnlyList<TimelineActionPresentationSnapshot> actionDisplaySnapshots)
        {
            return Prepare(BattleFlowHookRequest.EndTurn(
                sequence,
                occupiedCellCount,
                remainingHandInstanceIds,
                actionDisplaySnapshots));
        }

        public BattleFlowHookResult ExecutePrepared(
            long sequence,
            TurnLifecycleRequestKind requestKind)
        {
            if (_results.TryGetValue(sequence, out var completed))
            {
                LastResult = completed;
                return completed;
            }

            if (!_requests.TryGetValue(sequence, out var request))
            {
                return Complete(Failure(
                    sequence,
                    requestKind,
                    BattleFlowHookFailure.RequestNotPrepared,
                    "The lifecycle request was not frozen before the reserved hook."));
            }

            if (request.RequestKind != requestKind)
            {
                return Complete(Failure(
                    sequence,
                    requestKind,
                    BattleFlowHookFailure.RequestKindMismatch,
                    "The prepared request kind does not match the lifecycle context."));
            }

            var validationFailure = ValidateBeforeMutation(request);
            if (validationFailure != null)
            {
                return Complete(validationFailure);
            }

            DeckMoveResult discardResult = null;
            RoundTransactionResult roundResult = null;
            if (request.RequestKind == TurnLifecycleRequestKind.EndTurn)
            {
                discardResult = _deck.DiscardHand(
                    Command(sequence, "discard"),
                    request.RemainingHandInstanceIds);
                if (!discardResult.Succeeded)
                {
                    return Complete(Failure(
                        sequence,
                        requestKind,
                        BattleFlowHookFailure.DeckOperationFailed,
                        "Discard failed: " + discardResult.Reason + "."));
                }

                roundResult = _rounds.Apply(
                    RoundTransaction.AdvanceTurn(sequence, request.OccupiedCellCount));
                if (!roundResult.Succeeded)
                {
                    return Complete(Failure(
                        sequence,
                        requestKind,
                        BattleFlowHookFailure.RoundTransactionFailed,
                        "Round advance failed: " + roundResult.Failure + "."));
                }
            }

            var drawResult = _deck.Draw(
                Command(sequence, "draw"),
                DeckState.FormalDrawRequest);
            if (!drawResult.Succeeded &&
                drawResult.Reason != DeckOperationReason.Exhausted)
            {
                return Complete(Failure(
                    sequence,
                    requestKind,
                    BattleFlowHookFailure.DeckOperationFailed,
                    "Draw failed: " + drawResult.Reason + "."));
            }

            Current = Capture(request.ActionDisplaySnapshots);
            var result = new BattleFlowHookResult(
                sequence,
                requestKind,
                BattleFlowHookFailure.None,
                null,
                discardResult,
                roundResult,
                drawResult,
                Current);
            _results.Add(sequence, result);
            return Complete(result);
        }

        TurnLifecycleStepResult INextTurnHook.Run(TurnLifecycleContext context)
        {
            var result = ExecutePrepared(context.Sequence, context.RequestKind);
            return result.Succeeded
                ? TurnLifecycleStepResult.Successful
                : TurnLifecycleStepResult.Failed(result.FailureReason);
        }

        private BattleFlowHookResult Prepare(BattleFlowHookRequest request)
        {
            if (request.Sequence <= 0 ||
                request.OccupiedCellCount < 0 ||
                request.OccupiedCellCount > BattleRoundLedger.TimelineCellCount)
            {
                return Complete(Failure(
                    request.Sequence,
                    request.RequestKind,
                    BattleFlowHookFailure.InvalidRequest,
                    "The lifecycle sequence or frozen occupied-cell count is invalid."));
            }

            if (_results.TryGetValue(request.Sequence, out var completed))
            {
                return _requests[request.Sequence].HasSamePayload(request)
                    ? Complete(completed)
                    : Complete(Failure(
                        request.Sequence,
                        request.RequestKind,
                        BattleFlowHookFailure.SequenceConflict,
                        "The lifecycle sequence was already used with a different payload."));
            }

            if (_requests.TryGetValue(request.Sequence, out var prepared))
            {
                return prepared.HasSamePayload(request)
                    ? Complete(new BattleFlowHookResult(
                        request.Sequence,
                        request.RequestKind,
                        BattleFlowHookFailure.None,
                        null,
                        null,
                        null,
                        null,
                        Current))
                    : Complete(Failure(
                        request.Sequence,
                        request.RequestKind,
                        BattleFlowHookFailure.SequenceConflict,
                        "The lifecycle sequence was already prepared with a different payload."));
            }

            _requests.Add(request.Sequence, request);
            return Complete(new BattleFlowHookResult(
                request.Sequence,
                request.RequestKind,
                BattleFlowHookFailure.None,
                null,
                null,
                null,
                null,
                Current));
        }

        private BattleFlowHookResult ValidateBeforeMutation(BattleFlowHookRequest request)
        {
            if (_settlement.Snapshot.IsInputLocked)
            {
                return Failure(
                    request.Sequence,
                    request.RequestKind,
                    BattleFlowHookFailure.TerminalBattle,
                    "The battle is terminal and rejects further lifecycle hooks.");
            }

            if (request.RequestKind != TurnLifecycleRequestKind.EndTurn)
            {
                return null;
            }

            var round = _rounds.Snapshot;
            var award = BattleRoundLedger.TimelineCellCount - request.OccupiedCellCount;
            if (round.Timecoins > int.MaxValue - award ||
                (round.Phase == BattleRoundLedger.PhasesPerEra && round.Era == int.MaxValue))
            {
                return Failure(
                    request.Sequence,
                    request.RequestKind,
                    BattleFlowHookFailure.RoundTransactionFailed,
                    "Round advance failed: ArithmeticOverflow.");
            }

            return null;
        }

        private BattleFlowPresentationSnapshot Capture(
            IReadOnlyList<TimelineActionPresentationSnapshot> actionDisplaySnapshots)
        {
            return new BattleFlowPresentationSnapshot(
                _deck.Snapshot(),
                _rounds.Snapshot,
                _settlement.Snapshot,
                actionDisplaySnapshots);
        }

        private BattleFlowHookResult Failure(
            long sequence,
            TurnLifecycleRequestKind requestKind,
            BattleFlowHookFailure failure,
            string reason)
        {
            return new BattleFlowHookResult(
                sequence,
                requestKind,
                failure,
                reason,
                null,
                null,
                null,
                Current);
        }

        private BattleFlowHookResult Complete(BattleFlowHookResult result)
        {
            LastResult = result;
            return result;
        }

        private static DeckCommandId Command(long sequence, string operation)
        {
            return new DeckCommandId(
                "lifecycle:" + sequence + "/" + operation);
        }

        private static void AddStableIds(
            ICollection<string> destination,
            IReadOnlyList<CardInstance> cards)
        {
            for (var index = 0; index < cards.Count; index++)
            {
                destination.Add(cards[index].StableId);
            }
        }
    }
}
