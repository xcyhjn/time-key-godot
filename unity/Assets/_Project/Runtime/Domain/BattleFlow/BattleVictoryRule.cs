namespace TimeKey.Domain.BattleFlow
{
    public static class BattleVictoryRule
    {
        public static bool IsSatisfied(int currentHp, int maximumHp)
        {
            return maximumHp > 0 &&
                   currentHp >= 0 &&
                   (long)currentHp * 10L <= maximumHp;
        }
    }
}
