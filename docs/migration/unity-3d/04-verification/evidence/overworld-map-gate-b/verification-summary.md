# Overworld Map Gate B Verification Summary

> 状态：Gate B 完成
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 起始检查点：`cf530f751a2757a61cb7c0ae043bdf84037bea6f`

## 完成边界

- `OverworldRunApplication` 是唯一局外流程权威，内部继续只拥有一个
  `OverworldChapterState`；`OutOfBattleShellState` 降为既有 Presenter 的兼容投影。
- 新游戏/种子游戏用 Gate A 生成器建立地图。正式局外壳把中央战斗入口绑定为
  当前相邻 available 的真实 `chapter-..-layer-..-node-..` identity，不保留
  `combat-room-01` 翻译表。
- Battle/Boss 继续使用既有 `CombatLaunchPayload`/`CombatOutcome`。Event/Shop 使用
  独立 typed outcome；没有 Dictionary、object、Domain state 或 Unity object 进入
  cross-scene payload。
- Gate A 新增合法 `ExportState/Restore`，保存 revision、current/active、visited、
  settled、chapter advance 和 operation journal；同一操作恢复后仍可精确 replay，
  不同操作仍 conflict。
- 保存格式为 schema 2；schema 0/1 均迁移，future/corrupt/I/O failure 使用 typed
  结果。写入为 temp -> flush -> reload validation -> atomic replace，并保留 backup。
- SceneFlow 在源 Scene 卸载前执行 persistence prepare。写入或 unload 失败恢复旧
  Application、旧存档、源 Scene、焦点、遮罩和输入锁；成功卸载后 final commit
  只清除 pending record。
- MainMenu Continue 由真实、可恢复且 map fingerprint 一致的存档决定 enabled；
  Continue 恢复同一 run/map/current node/resources。磁盘恢复拒绝 active-room 中间态，
  内存 rollback copy 则保留 active combat。
- Boss victory commit plan 仍只推进一次；下一章节由同一 Application authority
  生成稳定新图。最终章节保留明确 final decision，不创建第二套章节状态机。

## 自动验证

| 门禁 | 结果 | 结构化证据 |
| --- | --- | --- |
| Overworld/Application/Persistence/StateStore EditMode | `68/68` | `editmode-overworld-application-persistence.xml` |
| SceneFlow + 02B4 EditMode 回归 | `100/100` | `editmode-sceneflow-02b4-regression.xml` |
| 真实 additive generated-map PlayMode | `5/5` | `playmode-additive-generated-map.xml` |
| 全 SceneFlow graphical D3D12 PlayMode | `17/17` | `playmode-sceneflow-regression.xml` |
| Era Clock EditMode 回归 | `17/17` | `editmode-era-clock-regression.xml` |
| Era Clock graphical D3D12 PlayMode 回归 | `13/13` | `playmode-era-clock-regression.xml` |

关键 additive 路径实际加载正式 Bootstrap/MainMenu/OutOfBattle/Combat：固定 seed 生成
相邻战斗节点，进入战斗，Victory/reward 返回并保存，随后用新 StateStore Continue
恢复同一 fingerprint/current node，并验证 outcome replay 幂等、不同 outcome conflict。

故障路径在 Combat 返回时注入 atomic replace 失败，结果停在
`UnloadingSource` 的源卸载之前；Combat、active room、旧存档、原焦点和输入全部恢复，
没有重复 Bootstrap、EventSystem、Transition、Audio 或 content entry。

## 证据哈希

- `editmode-overworld-application-persistence.xml`:
  `5FDEF64EA6828A6403E737D13B68E95D0272F439BA65A9A6C73D16B3A3F850E2`
- `editmode-sceneflow-02b4-regression.xml`:
  `6E84F8E35B38D33EF4E438DFA0A198E324CC67FF9989D83FB8AB50315EEB5EDB`
- `playmode-additive-generated-map.xml`:
  `0524F6F1B0C9BB38C0ABDF44F3E9A230602003D0D5C1A16A514205A6AE9476BC`
- `playmode-sceneflow-regression.xml`:
  `8656756741CB4D7DEBC8619E4D42A6B911C6BDC394528C41EDA9EC2BBCCC7728`
- `editmode-era-clock-regression.xml`:
  `4A3C08853226E7D2AD0683961AB802F4FDD1EAF07044229FC9726DB2BC5121FA`
- `playmode-era-clock-regression.xml`:
  `F122FF65C603A794A53D113A12527AB58E8B57D2F5A32D52ADEAB31B17AB529F`

## 下一门

Gate C authoring 正式动态地图 UI、节点全部状态、scroll/zoom/focus、Event/Shop
最小真实交互、Continue 错误提示和三视口实际渲染证据。Gate B 不宣称地图视觉完成。
