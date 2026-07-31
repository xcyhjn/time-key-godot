# 迁移验证 Harness

> 状态：首切片设计已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Godot 基线日志/截图、Unity Test Framework 1.6.0、首切片契约

## 分层验证

1. EditMode：卡牌解析、六边形映射、时间轴边界/冲突/顺序、伤害结果和结构化快照。
2. PlayMode：加载垂直切片场景，完成选卡、选目标、放置、结算，检查 3D 目标状态和 UI 文本。
3. Batchmode：导入/编译、运行 EditMode/PlayMode、打开验证场景并构建 Windows Player。
4. 渲染：Editor harness 用真实相机与 Canvas 渲染 1920×1080、1280×720 PNG；检查像素非空、主体入镜、文本无明显裁切或重叠。

## 固定夹具

```text
seed=731
card=lighting
timeline=12x3
playerOrigin=(0,0)
targetId=target-01
targetHp=10
enemyIntentOrigin=(2,1)
```

结构化快照必须包含 `turn`、`phase`、`timeline`、`targetHpBefore`、`targetHpAfter`、`enemyIntentResolved` 和 `seed`。字段顺序由 serializer 固定，便于 Git diff。

## 证据目录

```text
docs/migration/unity-3d/04-verification/evidence/
  godot-baseline/
  unity-slice-01/
```

日志、JUnit XML、截图和构建摘要均写入 `unity-slice-01/`。Unity 的 `Library/`、`Temp/`、`Logs/` 和实际 build 产物不进入 Git。

## 失败规则

- 编译或任一要求的测试失败：切片不得标记完成。
- batchmode 成功但无有效截图：只算结构验证，不算视觉验证。
- 截图空白、相机错误、主要控件裁切/重叠：修复后重跑两个视口。
- Godot 与 Unity 差异必须进入 parity matrix；不能用更新 golden snapshot 掩盖未知差异。
