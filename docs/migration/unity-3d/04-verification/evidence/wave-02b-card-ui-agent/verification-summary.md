# Wave 02B Card UI Agent 验证摘要

> 状态：通过
> 负责人：Wave 02B Agent 03
> 最后验证日期：2026-07-31
> 证据来源：Unity 6000.4.10f1 PlayMode、RenderTexture 实际渲染、人工截图检查

## 自动化结果

- Cards 专属 PlayMode：`4/4 passed`，`0 failed`，`0 skipped`。
- 当前全量 PlayMode：`12/12 passed`，包含 Cards `4`、既有战斗棋盘基线 `4`、并行 Board/Timeline Preview `4`。
- 结构化结果：`playmode-results.xml`、`playmode-full-results.xml`。
- 覆盖：hover、selected、pointer exit 后选中保持、右键/调用入口取消、事件只触发一次、拖动 started/moved/ended/cancelled、父级与局部姿态恢复、GraphicRaycaster 实际命中、pointer/scroll 消费、disabled 输入消费、重复 Build 不复制层级或监听。

## 实际渲染证据

| 视口 | Idle | Hover | Selected |
| --- | --- | --- | --- |
| 1920x1080 | `1920x1080-idle.png` | `1920x1080-hover.png` | `1920x1080-selected.png` |
| 1280x720 | `1280x720-idle.png` | `1280x720-hover.png` | `1280x720-selected.png` |
| 2560x1080 | `2560x1080-idle.png` | `2560x1080-hover.png` | `2560x1080-selected.png` |

渲染测试对每个状态执行局部卡牌区域像素检查：至少 32 种颜色，三态 checksum 必须不同；同时检查卡牌四角位于视口内，Selected 顶部低于视口高度 45%。

## 人工视觉检查

- `lighting` 卡框、雷击画面、中文标题、中文描述和四边完整显示，没有拆成仿制文本面板。
- Image 使用 Preserve Aspect；三种视口未见拉伸、裁切或横向超宽失真。
- Hover 相比 Idle 明确抬升和放大；Selected 进一步抬升、放大并显示金色描边，鼠标移出后仍可辨识。
- 1280x720 Selected 未越出底边或侵入视口上半区；2560x1080 保持居中，没有随宽屏横向拉伸。
- 当前证据使用 Prompt 允许的最小 Canvas；背景只是验证底板，不是共享战斗场景或最终棋盘构图。

## 已知边界

- 本 Agent 未修改共享场景、Controller 或 Editor harness，因此这些图片不能替代主智能体的 3D 棋盘集成截图。
- 当前素材 `.meta` 仍由主智能体统一裁决导入设置；测试从原 Texture2D 创建运行时 Sprite，再传给冻结的 Sprite view model。
- 原始 PNG 的不透明棋盘格像素按源文件保留；没有抠图或重制。
