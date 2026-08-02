using System.Globalization;

namespace TimeKey.Presentation.BattleFlow
{
    public static class BattleFlowChineseText
    {
        public const string DrawPile = "牌库";
        public const string Hand = "手牌";
        public const string DiscardPile = "弃牌";
        public const string Timecoins = "时间币";
        public const string Victory = "战斗胜利";
        public const string VictorySettled = "战斗已结算";
        public const string RewardClaimed = "奖励已领取";
        public const string Defeat = "战斗失败";
        public const string BattleEnded = "本次战斗已经结束";

        public static string Count(string label, int value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", label, value);
        }

        public static string Round(int era, int phase)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "纪元 {0}  阶段 {1}/8",
                era,
                phase);
        }
    }
}
