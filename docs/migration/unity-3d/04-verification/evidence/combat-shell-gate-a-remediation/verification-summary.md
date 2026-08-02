# Combat Shell Gate A review remediation verification

> 结论：PASS
> 日期：2026-08-02
> Unity：6000.4.10f1

## 审查项关闭

- source unload 是提交点：提交前失败回滚 target/source，提交后 reveal/unlock 失败保留 target，不再卸载两侧造成空场景。
- SceneFlow 使用合法 route/payload 类型矩阵；Empty payload 不能进入或离开 Combat。
- `BattleSettlementSnapshot` 携带 battle tag/seed，outcome 同时核对 launch、return 与 settlement identity；launch fingerprint 包含 battle tag。
- `SceneFlowStateStore` 在生产链消费 `OutOfBattleShellState`，校验 run/room/launch correlation；Binding 后的状态写入可回滚，source unload 启动后才提交。
- Bootstrap 初始化 fault 通过 `InitializationTask`/`InitializationException` 可观察，失败会卸载残留、揭罩并解锁。
- 全局输入锁同时覆盖 UI/EventSystem navigation 与三个直接轮询入口：战斗 controller、轨道相机和手牌 view。
- 锁在 drag capture 后生效时，手牌立即恢复父级/pose 且不再发布 moved/ended event。
- `GameOver -> MainMenu` 原子清理旧 run state，同一 Bootstrap 会话可进入不同 run。
- 增加 post-commit failure、typed bypass、跨战斗 settlement、错误 room/correlation、state rollback、真实 Bootstrap fault 和真实 additive bind failure 测试；所有 Task 等待均有 15 秒诊断超时。

## 最终门禁

| 门禁 | 结果 |
| --- | --- |
| SceneFlow 定向 EditMode | `30/30`，0 failed/skipped/inconclusive |
| full EditMode | `330/330`，0 failed/skipped/inconclusive |
| full graphical PlayMode | `64/64`，Direct3D 12，0 failed/skipped/inconclusive |
| Windows build | Development `Succeeded`，六 Scene，`211747089` bytes |
| actual Player | 1280x720 可见 Player，exit 0，marker 1，异常 0，Direct3D 12 |
| architecture | Application SceneFlow 无 UnityEngine/UnityEditor 引用；`git diff --check` 通过 |

正式证据为 `editmode-review-closure-final.xml`、`playmode-review-closure-rendered-final.xml`、`build-summary.json` 和 `player-smoke-summary.json`。失败或被参数污染的 XML、raw Unity/Player logs 不提交。

本整改不改变 Gate A 的无视觉壳范围，也不替代 Gate B 的 TopHUD/背景三视口与四向渲染证据。
