using System;
using TimeKey.Domain.BattleFlow;

namespace TimeKey.Application.SceneFlow
{
    public enum CombatOutcomeFailure
    {
        None,
        InvalidLaunch,
        InvalidSettlement,
        InvalidReturnPayload,
        LaunchMismatch,
        RewardNotClaimed,
        UnexpectedReward
    }

    public sealed class CombatOutcome : ISceneTransitionPayload
    {
        private CombatOutcome(
            string outcomeCorrelationId,
            CombatLaunchPayload launch,
            BattleReturnPayload returnPayload)
        {
            OutcomeCorrelationId = outcomeCorrelationId;
            LaunchCorrelationId = launch.LaunchCorrelationId;
            RunId = launch.RunId;
            RoomId = launch.RoomId;
            ReturnPayload = returnPayload;
        }

        public string OutcomeCorrelationId { get; }

        public string LaunchCorrelationId { get; }

        public string RunId { get; }

        public string RoomId { get; }

        public BattleReturnPayload ReturnPayload { get; }

        public SceneId TargetScene =>
            ReturnPayload.Outcome == BattleOutcome.VictorySettlement
                ? SceneId.OutOfBattleShell
                : SceneId.GameOver;

        public string Fingerprint =>
            "outcome:" + OutcomeCorrelationId + ":" + LaunchCorrelationId + ":" +
            RunId + ":" + RoomId + ":" + ReturnPayload.Outcome + ":" +
            ReturnPayload.Era + ":" + ReturnPayload.Phase + ":" +
            ReturnPayload.Timecoins + ":" + ReturnPayload.BattleTag + ":" +
            ReturnPayload.BattleSeed + ":" +
            string.Join(",", ReturnPayload.DeckStableIds);

        public static CombatOutcomeResult TryCreate(
            string outcomeCorrelationId,
            CombatLaunchPayload launch,
            BattleSettlementSnapshot settlement,
            BattleReturnPayload returnPayload)
        {
            if (string.IsNullOrWhiteSpace(outcomeCorrelationId) || launch == null)
            {
                return Failed(CombatOutcomeFailure.InvalidLaunch);
            }

            if (settlement == null)
            {
                return Failed(CombatOutcomeFailure.InvalidSettlement);
            }

            if (returnPayload == null)
            {
                return Failed(CombatOutcomeFailure.InvalidReturnPayload);
            }

            if (returnPayload.BattleTag != launch.BattleTag ||
                returnPayload.BattleSeed != launch.BattleSeed)
            {
                return Failed(CombatOutcomeFailure.LaunchMismatch);
            }

            if (returnPayload.Outcome == BattleOutcome.VictorySettlement &&
                !settlement.IsRewardClaimed)
            {
                return Failed(CombatOutcomeFailure.RewardNotClaimed);
            }

            if (returnPayload.Outcome == BattleOutcome.Defeat && settlement.IsRewardClaimed)
            {
                return Failed(CombatOutcomeFailure.UnexpectedReward);
            }

            if (settlement.Outcome != returnPayload.Outcome)
            {
                return Failed(CombatOutcomeFailure.InvalidSettlement);
            }

            return new CombatOutcomeResult(
                CombatOutcomeFailure.None,
                new CombatOutcome(outcomeCorrelationId, launch, returnPayload));
        }

        private static CombatOutcomeResult Failed(CombatOutcomeFailure failure)
        {
            return new CombatOutcomeResult(failure, outcome: null);
        }
    }

    public sealed class CombatOutcomeResult
    {
        internal CombatOutcomeResult(CombatOutcomeFailure failure, CombatOutcome outcome)
        {
            Failure = failure;
            Outcome = outcome;
        }

        public bool Succeeded => Failure == CombatOutcomeFailure.None;

        public CombatOutcomeFailure Failure { get; }

        public CombatOutcome Outcome { get; }
    }
}
