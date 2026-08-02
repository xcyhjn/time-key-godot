using System;
using System.Collections.Generic;

namespace TimeKey.Domain.BattleFlow
{
    public enum RoundTransactionKind
    {
        AdvanceTurn,
        SpendTimecoins
    }

    public enum RoundTransactionFailure
    {
        None,
        InvalidTransaction,
        InvalidSequence,
        InvalidOccupiedCellCount,
        InvalidAmount,
        InsufficientTimecoins,
        ArithmeticOverflow,
        SequenceConflict
    }

    public sealed class BattleRoundSnapshot
    {
        public BattleRoundSnapshot(int era, int phase, int timecoins)
        {
            if (era < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(era));
            }

            if (phase < 1 || phase > BattleRoundLedger.PhasesPerEra)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (timecoins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timecoins));
            }

            Era = era;
            Phase = phase;
            Timecoins = timecoins;
        }

        public int Era { get; }

        public int Phase { get; }

        public int Timecoins { get; }
    }

    public sealed class RoundTransaction
    {
        private RoundTransaction(
            long sequence,
            RoundTransactionKind kind,
            int occupiedCellCount,
            int amount)
        {
            Sequence = sequence;
            Kind = kind;
            OccupiedCellCount = occupiedCellCount;
            Amount = amount;
        }

        public long Sequence { get; }

        public RoundTransactionKind Kind { get; }

        public int OccupiedCellCount { get; }

        public int Amount { get; }

        public static RoundTransaction AdvanceTurn(long sequence, int occupiedCellCount)
        {
            return new RoundTransaction(
                sequence,
                RoundTransactionKind.AdvanceTurn,
                occupiedCellCount,
                amount: 0);
        }

        public static RoundTransaction SpendTimecoins(long sequence, int amount)
        {
            return new RoundTransaction(
                sequence,
                RoundTransactionKind.SpendTimecoins,
                occupiedCellCount: 0,
                amount);
        }

        internal bool HasSamePayload(RoundTransaction other)
        {
            return other != null &&
                   Kind == other.Kind &&
                   OccupiedCellCount == other.OccupiedCellCount &&
                   Amount == other.Amount;
        }
    }

    public sealed class RoundTransactionResult
    {
        internal RoundTransactionResult(
            long sequence,
            RoundTransactionKind kind,
            RoundTransactionFailure failure,
            BattleRoundSnapshot before,
            BattleRoundSnapshot after,
            int timecoinsAwarded,
            int timecoinsSpent)
        {
            Sequence = sequence;
            Kind = kind;
            Failure = failure;
            Before = before;
            After = after;
            TimecoinsAwarded = timecoinsAwarded;
            TimecoinsSpent = timecoinsSpent;
        }

        public bool Succeeded => Failure == RoundTransactionFailure.None;

        public long Sequence { get; }

        public RoundTransactionKind Kind { get; }

        public RoundTransactionFailure Failure { get; }

        public BattleRoundSnapshot Before { get; }

        public BattleRoundSnapshot After { get; }

        public int TimecoinsAwarded { get; }

        public int TimecoinsSpent { get; }
    }

    public sealed class BattleRoundLedger
    {
        public const int PhasesPerEra = 8;
        public const int TimelineCellCount = 36;

        private readonly Dictionary<long, TransactionJournalEntry> _journal =
            new Dictionary<long, TransactionJournalEntry>();

        private BattleRoundSnapshot _snapshot;

        public BattleRoundLedger(
            int initialEra = 1,
            int initialPhase = 1,
            int initialTimecoins = 0)
        {
            _snapshot = new BattleRoundSnapshot(
                initialEra,
                initialPhase,
                initialTimecoins);
        }

        public BattleRoundSnapshot Snapshot => _snapshot;

        public RoundTransactionResult Apply(RoundTransaction transaction)
        {
            if (transaction == null)
            {
                return Failed(
                    sequence: 0,
                    RoundTransactionKind.AdvanceTurn,
                    RoundTransactionFailure.InvalidTransaction);
            }

            if (transaction.Sequence <= 0)
            {
                return Failed(
                    transaction.Sequence,
                    transaction.Kind,
                    RoundTransactionFailure.InvalidSequence);
            }

            if (_journal.TryGetValue(transaction.Sequence, out var existing))
            {
                return existing.Transaction.HasSamePayload(transaction)
                    ? existing.Result
                    : Failed(
                        transaction.Sequence,
                        transaction.Kind,
                        RoundTransactionFailure.SequenceConflict);
            }

            var result = transaction.Kind == RoundTransactionKind.AdvanceTurn
                ? AdvanceTurn(transaction)
                : SpendTimecoins(transaction);
            _journal.Add(
                transaction.Sequence,
                new TransactionJournalEntry(transaction, result));
            return result;
        }

        private RoundTransactionResult AdvanceTurn(RoundTransaction transaction)
        {
            if (transaction.OccupiedCellCount < 0 ||
                transaction.OccupiedCellCount > TimelineCellCount)
            {
                return Failed(
                    transaction.Sequence,
                    transaction.Kind,
                    RoundTransactionFailure.InvalidOccupiedCellCount);
            }

            var award = TimelineCellCount - transaction.OccupiedCellCount;
            if (_snapshot.Timecoins > int.MaxValue - award)
            {
                return Failed(
                    transaction.Sequence,
                    transaction.Kind,
                    RoundTransactionFailure.ArithmeticOverflow);
            }

            var nextEra = _snapshot.Era;
            var nextPhase = _snapshot.Phase + 1;
            if (nextPhase > PhasesPerEra)
            {
                if (_snapshot.Era == int.MaxValue)
                {
                    return Failed(
                        transaction.Sequence,
                        transaction.Kind,
                        RoundTransactionFailure.ArithmeticOverflow);
                }

                nextEra++;
                nextPhase = 1;
            }

            var before = _snapshot;
            _snapshot = new BattleRoundSnapshot(
                nextEra,
                nextPhase,
                before.Timecoins + award);
            return new RoundTransactionResult(
                transaction.Sequence,
                transaction.Kind,
                RoundTransactionFailure.None,
                before,
                _snapshot,
                award,
                timecoinsSpent: 0);
        }

        private RoundTransactionResult SpendTimecoins(RoundTransaction transaction)
        {
            if (transaction.Amount <= 0)
            {
                return Failed(
                    transaction.Sequence,
                    transaction.Kind,
                    RoundTransactionFailure.InvalidAmount);
            }

            if (_snapshot.Timecoins < transaction.Amount)
            {
                return Failed(
                    transaction.Sequence,
                    transaction.Kind,
                    RoundTransactionFailure.InsufficientTimecoins);
            }

            var before = _snapshot;
            _snapshot = new BattleRoundSnapshot(
                before.Era,
                before.Phase,
                before.Timecoins - transaction.Amount);
            return new RoundTransactionResult(
                transaction.Sequence,
                transaction.Kind,
                RoundTransactionFailure.None,
                before,
                _snapshot,
                timecoinsAwarded: 0,
                transaction.Amount);
        }

        private RoundTransactionResult Failed(
            long sequence,
            RoundTransactionKind kind,
            RoundTransactionFailure failure)
        {
            return new RoundTransactionResult(
                sequence,
                kind,
                failure,
                _snapshot,
                _snapshot,
                timecoinsAwarded: 0,
                timecoinsSpent: 0);
        }

        private sealed class TransactionJournalEntry
        {
            public TransactionJournalEntry(
                RoundTransaction transaction,
                RoundTransactionResult result)
            {
                Transaction = transaction;
                Result = result;
            }

            public RoundTransaction Transaction { get; }

            public RoundTransactionResult Result { get; }
        }
    }
}
