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

        public static string LifecycleOccupantStatus(LifecycleOccupantChangeResult change)
        {
            if (change == null)
            {
                throw new System.ArgumentNullException(nameof(change));
            }

            var name = change.Reason == LifecycleMutationReason.TowerDecay
                ? "高塔"
                : GetEntityName(change.RuntimeId);
            if (change.Removed)
            {
                return name + " | 已移除";
            }

            if (change.AfterPoisonStacks > 0)
            {
                return name + " | 生命 " + change.AfterHp + " | 中毒 " +
                    change.AfterPoisonStacks + " 层";
            }

            return name + " | 生命 " + change.AfterHp;
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

        public static string GetCardEffectDescription(CardDefinition card)
        {
            if (card == null || card.Effects.Count == 0)
            {
                return "无效果";
            }

            var effect = card.Effects[0];
            switch (effect.Kind)
            {
                case CardEffectKind.Damage: return "造成 " + effect.Value + " 点伤害";
                case CardEffectKind.Elevation: return "范围内地块高度 +" + effect.Value;
                case CardEffectKind.Recover: return "恢复 " + effect.Value + " 点生命";
                case CardEffectKind.Built: return "建造高塔，初始生命 100";
                case CardEffectKind.Poison: return "施加 " + effect.Value + " 层中毒";
                case CardEffectKind.Clear: return "清除命中的完整时间轴行动";
                default: return effect.Kind.ToString();
            }
        }

        public static string GetCardPlacementDescription(CardDefinition card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            var timelineCells = card.Shape.Count > 0
                ? card.Shape.Count
                : card.Effects.Count > 0
                    ? card.Effects[0].ClearMask.Count
                    : 0;
            var target = card.Effects.Count > 0 && card.Effects[0].Kind == CardEffectKind.Clear
                ? "时间轴"
                : card.Effects.Count > 0 &&
                    (card.Effects[0].Kind == CardEffectKind.Elevation ||
                     card.Effects[0].Kind == CardEffectKind.Built)
                    ? "地块"
                    : "单位";
            return "目标：" + target + "  |  范围：" + card.Range.Count +
                " 格  |  时间轴：" + timelineCells + " 格";
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

        public static string GetEntityName(string entityId)
        {
            return entityId == "target-01" ? "目标 01" : "目标";
        }
    }
}
