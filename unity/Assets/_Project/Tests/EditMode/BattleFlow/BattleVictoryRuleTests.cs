using NUnit.Framework;
using TimeKey.Domain.BattleFlow;

namespace TimeKey.Tests.EditMode.BattleFlow
{
    public sealed class BattleVictoryRuleTests
    {
        [TestCase(0, 100, true)]
        [TestCase(10, 100, true)]
        [TestCase(11, 100, false)]
        [TestCase(1, 10, true)]
        [TestCase(0, 0, false)]
        [TestCase(-1, 100, false)]
        public void CurrentHealthUsesFrozenTenPercentBoundary(
            int currentHp,
            int maximumHp,
            bool expected)
        {
            Assert.That(
                BattleVictoryRule.IsSatisfied(currentHp, maximumHp),
                Is.EqualTo(expected));
        }

        [Test]
        public void CombatSliceFreezesTargetMaximumHealth()
        {
            var state = new TimeKey.Domain.CombatSliceState("target", 10, 731);

            Assert.That(state.TargetHp, Is.EqualTo(10));
            Assert.That(state.TargetMaxHp, Is.EqualTo(100));
        }
    }
}
