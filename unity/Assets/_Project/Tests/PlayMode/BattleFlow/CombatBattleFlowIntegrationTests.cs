using System.Collections;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Cards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.BattleFlow
{
    public sealed class CombatBattleFlowIntegrationTests
    {
        [UnityTest]
        public IEnumerator TwoFormalTurns_AwardTimecoinsAndRecycleDiscardWithShuffle()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.BattleFlow.Era, Is.EqualTo(1));
            Assert.That(controller.BattleFlow.Phase, Is.EqualTo(1));
            Assert.That(controller.BattleFlow.Timecoins, Is.Zero);
            Assert.That(controller.BattleFlow.DrawPile, Has.Count.EqualTo(7));
            Assert.That(controller.BattleFlow.Hand, Has.Count.EqualTo(5));
            Assert.That(controller.BattleFlow.DiscardPile, Is.Empty);

            var firstOccupiedCells = PlayRecoverTurn(controller);
            Assert.That(controller.BattleFlow.Phase, Is.EqualTo(2));
            Assert.That(
                controller.BattleFlow.Timecoins,
                Is.EqualTo(36 - firstOccupiedCells));
            Assert.That(controller.BattleFlow.DrawPile, Has.Count.EqualTo(2));
            Assert.That(controller.BattleFlow.Hand, Has.Count.EqualTo(5));
            Assert.That(controller.BattleFlow.DiscardPile, Has.Count.EqualTo(5));
            Assert.That(controller.LastBattleFlowResult.DrawResult.Shuffle.Occurred, Is.False);

            var secondOccupiedCells = PlayEarthquakeTurn(controller);
            Assert.That(controller.BattleFlow.Phase, Is.EqualTo(3));
            Assert.That(
                controller.BattleFlow.Timecoins,
                Is.EqualTo((36 - firstOccupiedCells) + (36 - secondOccupiedCells)));
            Assert.That(controller.BattleFlow.DrawPile, Has.Count.EqualTo(7));
            Assert.That(controller.BattleFlow.Hand, Has.Count.EqualTo(5));
            Assert.That(controller.BattleFlow.DiscardPile, Is.Empty);
            Assert.That(controller.LastBattleFlowResult.DrawResult.Shuffle.Occurred, Is.True);
        }

        [UnityTest]
        public IEnumerator Victory_LocksInputClaimsOneRewardAndCreatesTypedReturnBoundary()
        {
            yield return LoadSlice();
            var controller = GetController();
            PlayRecoverTurn(controller);

            Assert.That(controller.SelectCard(VerticalSliceController.LightingCardId), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            Assert.That(controller.ResolveTimeline().TargetHpAfter, Is.Zero);

            Assert.That(
                controller.BattleFlow.Settlement.Outcome,
                Is.EqualTo(BattleOutcome.VictorySettlement));
            Assert.That(controller.BattleFlow.IsInputLocked, Is.True);
            foreach (var card in controller.CardHandHost.Cards)
            {
                Assert.That(
                    card.InteractionState,
                    Is.EqualTo(CardHandInteractionState.Disabled));
            }
            Assert.That(controller.SelectCard("recover"), Is.False);

            var settlement = Object.FindFirstObjectByType<BattleSettlementPresenter>();
            Assert.That(settlement, Is.Not.Null);
            Assert.That(settlement.IsVisible, Is.True);
            Assert.That(settlement.IsRewardVisible, Is.True);
            var rewardButton = GameObject.Find("RewardButton").GetComponent<Button>();
            rewardButton.onClick.Invoke();
            rewardButton.onClick.Invoke();
            Assert.That(controller.BattleFlow.Settlement.IsRewardClaimed, Is.True);
            Assert.That(settlement.IsRewardVisible, Is.False);

            var boundary = controller.CreateBattleReturnBoundary();
            Assert.That(boundary.Succeeded, Is.True);
            Assert.That(boundary.Payload.Outcome, Is.EqualTo(BattleOutcome.VictorySettlement));
            Assert.That(boundary.Payload.Completion, Is.EqualTo(BattleReturnCompletion.VictoryCompleted));
            Assert.That(boundary.Payload.DeckStableIds, Has.Count.EqualTo(12));
            Assert.That(boundary.Payload.BattleTag, Is.EqualTo("combat-vertical-slice"));
            Assert.That(boundary.Payload.BattleSeed, Is.EqualTo(VerticalSliceController.FixtureSeed));
        }

        [UnityTest]
        public IEnumerator Defeat_HasNoRewardAndCreatesTypedDefeatReturnBoundary()
        {
            yield return LoadSlice();
            var controller = GetController();

            var settlement = controller.ResolveBattleOutcome(BattleOutcome.Defeat);
            Assert.That(settlement.Succeeded, Is.True);
            Assert.That(controller.BattleFlow.IsInputLocked, Is.True);
            Assert.That(controller.BattleFlow.Settlement.RewardEntry, Is.Null);
            Assert.That(controller.SelectCard("poison"), Is.False);

            var presenter = Object.FindFirstObjectByType<BattleSettlementPresenter>();
            Assert.That(presenter.IsVisible, Is.True);
            Assert.That(presenter.IsRewardVisible, Is.False);

            var boundary = controller.CreateBattleReturnBoundary();
            Assert.That(boundary.Succeeded, Is.True);
            Assert.That(boundary.Payload.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(boundary.Payload.Completion, Is.EqualTo(BattleReturnCompletion.Defeat));
            Assert.That(boundary.Payload.IsCompleted, Is.False);
        }

        private static int PlayRecoverTurn(VerticalSliceController controller)
        {
            Assert.That(controller.SelectCard("recover"), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            var occupiedCells = controller.TimelineOccupiedCellCount;
            Assert.That(controller.ResolveTimeline(), Is.Not.Null);
            return occupiedCells;
        }

        private static int PlayEarthquakeTurn(VerticalSliceController controller)
        {
            Assert.That(
                controller.SelectCard(VerticalSliceController.EarthquakeCardId),
                Is.True);
            Assert.That(controller.SelectEarthquakeTarget(new HexCoord(0, 0)), Is.True);
            Assert.That(controller.PreviewTimelineSelected(0, 0), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            var occupiedCells = controller.TimelineOccupiedCellCount;
            Assert.That(controller.ResolveTimeline(), Is.Not.Null);
            return occupiedCells;
        }

        private static IEnumerator LoadSlice()
        {
            SceneManager.LoadScene("CombatVerticalSlice", LoadSceneMode.Single);
            yield return null;
        }

        private static VerticalSliceController GetController()
        {
            var root = GameObject.Find("VerticalSliceRoot");
            Assert.That(root, Is.Not.Null);
            var controller = root.GetComponent<VerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            return controller;
        }
    }
}
