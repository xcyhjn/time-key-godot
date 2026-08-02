# Wave 02B4 Gate D 视觉证据审查

## 结论

**总判定：PASS WITH CONCERNS**

最终审查对象是 `deck-battle-flow-gate-d` 中 2026-08-02 15:46 重捕的 18 张 PNG，而不是同日 14:55 的旧批次。全部图片均通过 `view_image` 逐张检查。

当前底部物理手牌和权威 HUD 一致：常规状态显示 5 张卡；两张 committed-action-frame 图片显示 4 张卡，同时 HUD 为 `手牌 4 / 弃牌 6`。对应 CardInstance 已离手，而 timeline action frame 与左侧保存的动作详情仍可见，因此 display payload 不依赖已销毁的 hand View。

三种视口均没有 UI 越界、结算面板裁切、中文 tofu 或不连贯的 Silver 字形。初始 `7/5/0`、第二回合 `2/5/5`、第三回合洗回后的 `7/5/0`，以及时间币 `0 -> 31 -> 63` 均可直接读取。胜利奖励入口、领取后按钮消失、失败无奖励也有直接证据。

唯一非阻塞注意项：自动化截图会保留选中卡的抬升状态，局部遮挡相邻卡牌上缘；但标题/效果的主要内容仍可读，卡牌未越出手牌容器或视口。

## 覆盖判定

| 验收要素 | 判定 | 最终批次依据 |
| --- | --- | --- |
| 1280x720 / 1920x1080 / 2560x1080 | PASS | HUD、timeline、棋盘、手牌与结算层均未越界或裁切 |
| 中文与 Silver 视觉 | PASS | 中文正文无 tofu，字形观感一致；资产绑定另由 Scene/Prefab EditMode 验证 |
| 牌库/手牌/弃牌 | PASS | `7/5/0 -> 2/5/5 -> 7/5/0`，物理手牌为 5 张 |
| Era / phase | PASS | `纪元 1，阶段 1/8 -> 2/8 -> 3/8` |
| 时间币 | PASS | `0 -> 31 -> 63`，胜利结算为 64 |
| 确定性洗回与空弃牌 | PASS | 第二回合弃牌 5，第三回合牌库 7、弃牌 0 |
| action frame 独立存续 | PASS | 提交后手牌 4、弃牌 6；卡牌消失但青色 frame 和左侧动作详情保留 |
| 胜利奖励一次性入口 | PASS | 奖励按钮可见；领取后显示“奖励已领取”且按钮消失 |
| 胜负互斥视觉 | PASS | 胜利与失败分别展示，失败三视口没有奖励入口 |
| 响应式手牌可读性 | CONCERNS | 无裁切；抬升卡会局部遮挡相邻卡牌上缘 |

## 逐组复核

- `initial-empty-discard-*`：5 张手牌，空弃牌，资源 0，三视口稳定。
- `round-2-draw-discard-*`：5 张手牌，牌库 2、弃牌 5、时间币 31。
- `round-2-committed-action-frame-*`：4 张手牌，已提交 action frame 与保存详情同时存在。
- `round-3-post-shuffle-empty-discard-*`：5 张手牌，牌库 7、弃牌 0、时间币 63。
- `victory-*`：提交时 4 张手牌；结算后奖励入口和领取完成状态正确。
- `defeat-no-reward-*`：5 张手牌，失败面板居中且无奖励按钮。

## 证据边界

PNG 能证明实际渲染和响应式布局，不能单独证明 Domain 权威性或字体 GUID。后者由完整 EditMode、graphical PlayMode、Scene/Prefab 引用测试以及 Player smoke 共同闭合。
