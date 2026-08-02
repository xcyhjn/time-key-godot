using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;

namespace TimeKey.Application.BattleFlow
{
    public enum BattleFlowHookFailure
    {
        None,
        InvalidRequest,
        SequenceConflict,
        RequestNotPrepared,
        RequestKindMismatch,
        TerminalBattle,
        RoundTransactionFailed,
        DeckOperationFailed
    }

    public sealed class BattleFlowHookRequest
    {
        private readonly ReadOnlyCollection<CardInstanceId> _remainingHandInstanceIds;
        private readonly ReadOnlyCollection<TimelineActionPresentationSnapshot>
            _actionDisplaySnapshots;

        private BattleFlowHookRequest(
            long sequence,
            TurnLifecycleRequestKind requestKind,
            int occupiedCellCount,
            IReadOnlyList<CardInstanceId> remainingHandInstanceIds,
            IReadOnlyList<TimelineActionPresentationSnapshot> actionDisplaySnapshots)
        {
            Sequence = sequence;
            RequestKind = requestKind;
            OccupiedCellCount = occupiedCellCount;
            _remainingHandInstanceIds = Copy(remainingHandInstanceIds);
            _actionDisplaySnapshots = CopySnapshots(actionDisplaySnapshots);
        }

        public long Sequence { get; }

        public TurnLifecycleRequestKind RequestKind { get; }

        public int OccupiedCellCount { get; }

        public IReadOnlyList<CardInstanceId> RemainingHandInstanceIds =>
            _remainingHandInstanceIds;

        public IReadOnlyList<TimelineActionPresentationSnapshot> ActionDisplaySnapshots =>
            _actionDisplaySnapshots;

        public static BattleFlowHookRequest InitialStart(long sequence)
        {
            return new BattleFlowHookRequest(
                sequence,
                TurnLifecycleRequestKind.InitialStart,
                occupiedCellCount: 0,
                Array.Empty<CardInstanceId>(),
                Array.Empty<TimelineActionPresentationSnapshot>());
        }

        public static BattleFlowHookRequest EndTurn(
            long sequence,
            int occupiedCellCount,
            IReadOnlyList<CardInstanceId> remainingHandInstanceIds,
            IReadOnlyList<TimelineActionPresentationSnapshot> actionDisplaySnapshots)
        {
            return new BattleFlowHookRequest(
                sequence,
                TurnLifecycleRequestKind.EndTurn,
                occupiedCellCount,
                remainingHandInstanceIds ??
                    throw new ArgumentNullException(nameof(remainingHandInstanceIds)),
                actionDisplaySnapshots ??
                    throw new ArgumentNullException(nameof(actionDisplaySnapshots)));
        }

        internal bool HasSamePayload(BattleFlowHookRequest other)
        {
            if (other == null ||
                RequestKind != other.RequestKind ||
                OccupiedCellCount != other.OccupiedCellCount ||
                _remainingHandInstanceIds.Count != other._remainingHandInstanceIds.Count ||
                _actionDisplaySnapshots.Count != other._actionDisplaySnapshots.Count)
            {
                return false;
            }

            for (var index = 0; index < _remainingHandInstanceIds.Count; index++)
            {
                if (_remainingHandInstanceIds[index] != other._remainingHandInstanceIds[index])
                {
                    return false;
                }
            }

            for (var index = 0; index < _actionDisplaySnapshots.Count; index++)
            {
                if (_actionDisplaySnapshots[index].ActionId !=
                    other._actionDisplaySnapshots[index].ActionId)
                {
                    return false;
                }
            }

            return true;
        }

        private static ReadOnlyCollection<CardInstanceId> Copy(
            IReadOnlyList<CardInstanceId> source)
        {
            var copied = new List<CardInstanceId>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                if (!source[index].IsValid)
                {
                    throw new ArgumentException(
                        "Remaining hand identities must be valid.",
                        nameof(source));
                }

                copied.Add(source[index]);
            }

            return new ReadOnlyCollection<CardInstanceId>(copied);
        }

        private static ReadOnlyCollection<TimelineActionPresentationSnapshot> CopySnapshots(
            IReadOnlyList<TimelineActionPresentationSnapshot> source)
        {
            var copied = new List<TimelineActionPresentationSnapshot>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                copied.Add(source[index] ??
                    throw new ArgumentException(
                        "Action display snapshots cannot contain null values.",
                        nameof(source)));
            }

            return new ReadOnlyCollection<TimelineActionPresentationSnapshot>(copied);
        }
    }

    public sealed class BattleFlowPresentationSnapshot
    {
        private readonly ReadOnlyCollection<TimelineActionPresentationSnapshot>
            _actionDisplaySnapshots;

        internal BattleFlowPresentationSnapshot(
            DeckSnapshot deck,
            BattleRoundSnapshot round,
            BattleSettlementSnapshot settlement,
            IReadOnlyList<TimelineActionPresentationSnapshot> actionDisplaySnapshots)
        {
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Round = round ?? throw new ArgumentNullException(nameof(round));
            Settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
            _actionDisplaySnapshots = Copy(actionDisplaySnapshots);
        }

        public DeckSnapshot Deck { get; }

        public BattleRoundSnapshot Round { get; }

        public BattleSettlementSnapshot Settlement { get; }

        public IReadOnlyList<CardInstance> DrawPile => Deck.DrawPile;

        public IReadOnlyList<CardInstance> Hand => Deck.Hand;

        public IReadOnlyList<CardInstance> DiscardPile => Deck.DiscardPile;

        public int Era => Round.Era;

        public int Phase => Round.Phase;

        public int Timecoins => Round.Timecoins;

        public BattleOutcome Outcome => Settlement.Outcome;

        public bool IsInputLocked => Settlement.IsInputLocked;

        public IReadOnlyList<TimelineActionPresentationSnapshot> ActionDisplaySnapshots =>
            _actionDisplaySnapshots;

        private static ReadOnlyCollection<TimelineActionPresentationSnapshot> Copy(
            IReadOnlyList<TimelineActionPresentationSnapshot> source)
        {
            var copied = new List<TimelineActionPresentationSnapshot>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                copied.Add(source[index]);
            }

            return new ReadOnlyCollection<TimelineActionPresentationSnapshot>(copied);
        }
    }

    public sealed class BattleFlowHookResult
    {
        internal BattleFlowHookResult(
            long sequence,
            TurnLifecycleRequestKind requestKind,
            BattleFlowHookFailure failure,
            string failureReason,
            DeckMoveResult discardResult,
            RoundTransactionResult roundResult,
            DeckMoveResult drawResult,
            BattleFlowPresentationSnapshot snapshot)
        {
            Sequence = sequence;
            RequestKind = requestKind;
            Failure = failure;
            FailureReason = failureReason;
            DiscardResult = discardResult;
            RoundResult = roundResult;
            DrawResult = drawResult;
            Snapshot = snapshot;
        }

        public bool Succeeded => Failure == BattleFlowHookFailure.None;

        public long Sequence { get; }

        public TurnLifecycleRequestKind RequestKind { get; }

        public BattleFlowHookFailure Failure { get; }

        public string FailureReason { get; }

        public DeckMoveResult DiscardResult { get; }

        public RoundTransactionResult RoundResult { get; }

        public DeckMoveResult DrawResult { get; }

        public BattleFlowPresentationSnapshot Snapshot { get; }
    }
}
