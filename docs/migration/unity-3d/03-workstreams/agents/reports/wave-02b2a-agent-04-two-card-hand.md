# Wave 02B2A Agent 04 两卡手牌报告

> 状态：实现与组件门禁完成；所有权交回主智能体
> 负责人：Wave 02B2A Agent 04
> 最后验证日期：2026-08-01

## 结论

新增 `CardHandHost`，继续复用两个独立 `CardHandView`。Host 只持有有序 `CardViewModel`、stable ID 与交互状态，不引用 `CardDefinition`，不解析效果，也不判断地图目标、范围或时间轴合法性。既有单卡 `CardHandView.Build` 和事件 API 未修改。

## 实际改动

- `unity/Assets/_Project/Runtime/Presentation/Cards/CardHandHost.cs` 及 Unity 生成的 `.meta`
- `unity/Assets/_Project/Tests/PlayMode/Cards/CardHandHostTests.cs` 及 `.meta`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/**`
- 本报告

未修改 Controller、Targeting、Terrain、Scene、Editor、Domain、Infrastructure、Resources、asmdef、ProjectSettings 或共享文档；未暂存、commit、push、切分支或回退共享工作树。

## 组件与 API

```text
CardHandHost.Build(IReadOnlyList<CardViewModel>)
CardHandHost.Cards : IReadOnlyList<CardHandView>
CardHandHost.CardCount : int
CardHandHost.SelectedStableId : string
CardHandHost.GetCard(stableId) -> CardHandView
CardHandHost.RequestCancelSelectedCard() -> bool
CardHandHost.SetInteractionState(state)
CardHandHost.ApplyVisualStateImmediate()

event CardSelected(stableId)
event CardCancelRequested(stableId)
event CardDragChanged(stableId, pointerPosition, phase)
```

`Build` 按输入顺序复用 stable ID 对应的单卡 view，移除已不在输入中的 view，并拒绝 null、重复 ID 或多张初始 selected；重复调用不会复制层级或监听。布局由底部 stretch host、受限槽位宽度和 `102..116 px` 响应式间距组成，没有写死 1920 坐标。选中第二张时，host 先调用第一张既有取消路径，使其恢复 idle pose 并发出一次 cancel，再把新卡提到最后 sibling。

## 事件与测试

Cards PlayMode 子集为 `9/9 passed`、`0 failed`、`0 skipped`：既有单卡回归 `4/4`，新增 host `5/5`。

- 连续选择 `lighting -> earthquake`：selected 各 1 次，`lighting` cancel 1 次；右键取消后 `earthquake` cancel 1 次。
- lighting drag：`Started/Moved/Ended` 各 1 次，三个事件 stable ID 均为 `lighting`，结束后恢复原 parent。
- Disabled 状态下 pointer down/click/scroll 均被消费，selected 事件为 0。
- 重复 `Build` 后仍为 2 个 view、原实例复用、后代节点数不变。

结构化结果：`docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/playmode-results.xml`。

Unity 启动时日志出现 LicenseClient 首次 handshake 失败和 public CDN timeout，但后续许可证成功，进程退出码 0，测试 XML 完整且 9/9 通过；没有编译或测试失败。

## 实际渲染检查

已生成并逐张查看 9 张 PNG：`1280x720`、`1920x1080`、`2560x1080` 各有 idle、lighting-selected、earthquake-selected。两张真实原图均完整入镜且保持 `125:175` 比例；idle 轻微重叠但标题、插画可辨，选中卡无底边/视口裁切并位于上层，三次状态 rebuild 后无布局漂移。

该独立 Cards rig 没有真实棋盘/时间轴，只能确认 host 位于底部且卡顶不超过视口高度 50%。最终棋盘与时间轴遮挡必须由主智能体在共享战斗场景接线后再次截图检查，不能由本组件证据替代。

## 主 Controller 接线示例

```csharp
var handObject = new GameObject("CardHandHost", typeof(RectTransform));
handObject.transform.SetParent(canvas.transform, false);
var hand = handObject.AddComponent<CardHandHost>();
hand.Build(new[]
{
    new CardViewModel("lighting", lightingSprite, false, true),
    new CardViewModel("earthquake", earthquakeSprite, false, true)
});
hand.CardSelected += stableId => SelectCard(stableId);
hand.CardCancelRequested += HandleCardCancelRequested;
hand.CardDragChanged += HandleCardDragChanged;
```

Controller 可继续用 `hand.GetCard("lighting")` 取得旧单卡兼容引用；进入 Targeting/Scheduling 时调用 `hand.SetInteractionState(...)`，退出流程通过重新 `Build` 两张未选中 model 或取消当前卡清除选择。Host 不应接收 `CardDefinition` 或复制 effect/target/timeline 规则。

## Git 与保护

专属路径 `git diff --check` 通过。Unity/Godot 进程在测试后均为空。四个用户原有 Godot 未提交文件仍保持未暂存，本 Agent 未触碰。
