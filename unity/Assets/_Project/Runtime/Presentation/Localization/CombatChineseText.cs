using TimeKey.Domain;

namespace TimeKey.Presentation.Localization
{
    public static class CombatChineseText
    {
        public const string SceneTitle = "时之钥 / 战斗棋盘";
        public const string DefaultTargetStatus = "目标 01 | 生命 10 / 10";
        public const string EnemyIntentDetail = "敌方意图 / 第 03 格·第二行";
        public const string ResolveTimeline = "结算时间轴";
        public const string ClearSelected = "已选择清除牌 | 请选择时间轴位置";
        public const string CardSelected = "已选择卡牌 | 请选择目标";
        public const string TargetLocked = "目标已锁定 | 请选择时间轴位置";
        public const string ClearPositionValid = "清除位置有效 | 点击确认";
        public const string ClearPositionOutOfBounds = "清除范围超出时间轴";
        public const string TimelinePositionValid = "时间轴位置有效 | 点击确认";
        public const string TimelinePositionInvalid = "时间轴位置无效";
        public const string ActionPlaced = "行动已放置 | 请结算时间轴";
        public const string CardCancelled = "已取消卡牌 | 请选择卡牌";
        public const string ClearResolved = "清除完成 | 时间轴已更新";
        public const string Resolved = "结算完成 | 时间轴已推进";
        public const string SessionClosed = "战斗已结束";
        public const string SelectCard = "请选择卡牌";
        public const string ClearNoMapTarget = "清除牌 | 无需地图目标";
        public const string NoTarget = "未选择目标";
        public const string ClearHit = "命中";
        public const string EnemyIntentTimelineLabel = "意图";

        public static string ClearRemoved(int count) => "清除完成 | 已移除 " + count + " 个行动";

        public static string ClearHits(int count) => "清除预览 | 命中 " + count + " 个行动";

        public static string PoisonStatus(string creationId, string runtimeId, int stacks)
        {
            return GetOccupantName(creationId, runtimeId) + " | 中毒 " + stacks + " 层";
        }

        public static string OccupantHealth(string creationId, string runtimeId, int hp)
        {
            return GetOccupantName(creationId, runtimeId) + " | 生命 " + hp;
        }

        public static string TargetHealth(int hp) => "目标 | 生命 " + hp;

        public static string EntityLocked(string entityId) => GetEntityName(entityId) + " | 已锁定";

        public static string TileLocked(HexCoord coordinate)
        {
            return "地块 " + coordinate.Q + "," + coordinate.R + " | 已锁定";
        }

        public static string TileSelected(HexCoord coordinate)
        {
            return "已选择地块 " + coordinate.Q + "," + coordinate.R;
        }

        public static string CardHeld(string stableId)
        {
            return "正持有" + GetCardName(stableId) + " | 请选择目标和时间轴位置";
        }

        public static string ClearFailed(string stableId) => GetCardName(stableId) + "未能清除时间轴";

        public static string CommitFailed(string stableId) => GetCardName(stableId) + "未能放入时间轴";

        public static string GetTimelineLabel(string stableId)
        {
            return stableId == "enemy-intent" ? EnemyIntentTimelineLabel : GetCardName(stableId);
        }

        public static string GetCardName(string stableId)
        {
            switch (stableId)
            {
                case "lighting": return "雷击";
                case "earthquake": return "地震";
                case "wind": return "台风";
                case "recover": return "恢复";
                case "tower": return "高塔";
                case "poison": return "中毒";
                case "tornado": return "龙卷风";
                default: return "未知卡牌";
            }
        }

        private static string GetOccupantName(string creationId, string runtimeId)
        {
            return creationId == "tower" ? "高塔" : GetEntityName(runtimeId);
        }

        private static string GetEntityName(string entityId)
        {
            return entityId == "target-01" ? "目标 01" : "目标";
        }
    }
}
