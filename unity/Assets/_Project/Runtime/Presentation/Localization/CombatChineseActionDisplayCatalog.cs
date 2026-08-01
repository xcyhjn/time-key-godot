using System;
using TimeKey.Application;
using TimeKey.Domain;

namespace TimeKey.Presentation.Localization
{
    public sealed class CombatChineseActionDisplayCatalog : IActionDisplayCatalog
    {
        public static CombatChineseActionDisplayCatalog Instance { get; } =
            new CombatChineseActionDisplayCatalog();

        private CombatChineseActionDisplayCatalog()
        {
        }

        public TimelineActionDisplayPayload GetDisplay(TimelineAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var description = action.ActorKind == TimelineActorKind.Enemy && action.Effects.Count == 0
                ? "源命令暂不支持，本轮不产生效果"
                : DescribeEffects(action);
            return new TimelineActionDisplayPayload(
                CombatChineseText.GetTimelineLabel(action.CardId),
                description,
                action.CardId,
                BuildSourceLabel(action),
                BuildTargetLabel(action));
        }

        private static string BuildSourceLabel(TimelineAction action)
        {
            return action.SourceId == null
                ? null
                : "来源：" + CombatChineseText.GetEntityName(action.SourceId);
        }

        private static string BuildTargetLabel(TimelineAction action)
        {
            if (action.ActorKind == TimelineActorKind.Enemy &&
                string.Equals(action.SourceId, action.TargetId, StringComparison.Ordinal))
            {
                return "目标：自身";
            }

            if (action.CardId == "earthquake" || action.CardId == "tower")
            {
                return "目标：地块";
            }

            return string.IsNullOrWhiteSpace(action.TargetId)
                ? null
                : "目标：" + CombatChineseText.GetEntityName(action.TargetId);
        }

        private static string DescribeEffects(TimelineAction action)
        {
            if (action.Effects.Count == 0)
            {
                return "无效果";
            }

            var effect = action.Effects[0];
            switch (effect.Kind)
            {
                case CardEffectKind.Damage:
                    return "造成 " + effect.Value + " 点伤害";
                case CardEffectKind.Elevation:
                    return "范围内地块高度 +" + effect.Value;
                case CardEffectKind.Recover:
                    return "恢复 " + effect.Value + " 点生命";
                case CardEffectKind.Built:
                    return effect.CreationId == "tower" ? "建造高塔" : "建造构筑物";
                case CardEffectKind.Poison:
                    return "施加 " + effect.Value + " 层中毒";
                case CardEffectKind.Clear:
                    return "清除时间轴行动";
                default:
                    return "未知效果";
            }
        }
    }
}
