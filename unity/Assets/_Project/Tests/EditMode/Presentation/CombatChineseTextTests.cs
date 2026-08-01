using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Localization;

namespace TimeKey.Tests.EditMode.Presentation
{
    public sealed class CombatChineseTextTests
    {
        [TestCase("lighting", "雷击")]
        [TestCase("earthquake", "地震")]
        [TestCase("wind", "台风")]
        [TestCase("recover", "恢复")]
        [TestCase("tower", "高塔")]
        [TestCase("poison", "中毒")]
        [TestCase("tornado", "龙卷风")]
        public void GetCardName_MapsEveryCurrentCard(string stableId, string expected)
        {
            Assert.That(CombatChineseText.GetCardName(stableId), Is.EqualTo(expected));
        }

        [Test]
        public void PlayerFacingCatalog_ContainsNoEnglishLetters()
        {
            var values = new[]
            {
                CombatChineseText.SceneTitle,
                CombatChineseText.DefaultTargetStatus,
                CombatChineseText.EnemyIntentDetail,
                CombatChineseText.ResolveTimeline,
                CombatChineseText.ClearSelected,
                CombatChineseText.CardSelected,
                CombatChineseText.TargetLocked,
                CombatChineseText.ClearPositionValid,
                CombatChineseText.ClearPositionOutOfBounds,
                CombatChineseText.TimelinePositionValid,
                CombatChineseText.TimelinePositionInvalid,
                CombatChineseText.ActionPlaced,
                CombatChineseText.CardCancelled,
                CombatChineseText.ClearResolved,
                CombatChineseText.Resolved,
                CombatChineseText.SessionClosed,
                CombatChineseText.SelectCard,
                CombatChineseText.ClearNoMapTarget,
                CombatChineseText.NoTarget,
                CombatChineseText.ClearHit,
                CombatChineseText.EnemyIntentTimelineLabel,
                CombatChineseText.ClearRemoved(1),
                CombatChineseText.ClearHits(1),
                CombatChineseText.PoisonStatus(null, "target-01", 2),
                CombatChineseText.OccupantHealth("tower", "tower-01", 100),
                CombatChineseText.TargetHealth(0),
                CombatChineseText.EntityLocked("target-01"),
                CombatChineseText.TileLocked(new HexCoord(0, 0)),
                CombatChineseText.TileSelected(new HexCoord(0, 0)),
                CombatChineseText.CardHeld("lighting"),
                CombatChineseText.ClearFailed("wind"),
                CombatChineseText.CommitFailed("lighting")
            };

            for (var index = 0; index < values.Length; index++)
            {
                Assert.That(values[index], Does.Not.Match("[A-Za-z]"), values[index]);
            }
        }

        [Test]
        public void ActionDisplayCatalog_LocalizesRuntimeIdentities()
        {
            var enemy = new TimelineAction(
                new TimelineActionIdentity("cycle:1/action:0"),
                TimelineActorKind.Enemy,
                0,
                "target-01",
                new HexCoord(0, 0),
                "enemy-intent",
                "target-01",
                new TimelineCell(2, 1),
                new[] { new TimelineCell(0, 0) },
                new HexCoord(0, 0),
                System.Array.Empty<CardEffect>(),
                System.Array.Empty<HexCoord>());

            var display = CombatChineseActionDisplayCatalog.Instance.GetDisplay(enemy);

            Assert.That(display.SourceLabel, Is.EqualTo("来源：目标 01"));
            Assert.That(display.TargetLabel, Is.EqualTo("目标：自身"));
            Assert.That(display.Description, Is.EqualTo("源命令暂不支持，本轮不产生效果"));
            StringAssert.DoesNotContain("target-01", display.SourceLabel + display.TargetLabel);
        }
    }
}
