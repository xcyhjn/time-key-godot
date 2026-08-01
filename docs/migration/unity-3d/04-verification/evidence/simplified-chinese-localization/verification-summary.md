# Unity 战斗切片简体中文验证总结

> 结果：PASS
> 日期：2026-08-02
> 语言：`zh-CN`
> 字体：Silver

## 覆盖范围

本证据刷新玩家可见文本、字体、Scene/TimelineCell Prefab、Presenter/Controller 状态、时间轴短标签和 Windows Player。stable ID、JSON 字段、节点名、异常与诊断日志保持英文，不属于玩家文案。

七张卡面原本已经是中文美术，本阶段没有改图或改 stable ID；运行时显示名统一为雷击、地震、台风、恢复、高塔、中毒、龙卷风。

## 自动化结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| EditMode | `161/161` passed，0 failed，0 skipped | `editmode-results.xml` |
| PlayMode | `38/38` passed，0 failed，0 skipped | `playmode-results.xml` |
| 汉化 Harness | marker=`TIMEKEY_SIMPLIFIED_CHINESE_HARNESS_PASS` | `localization-summary.json` |
| Windows build | `Succeeded`，`210916374` bytes | `localization-summary.json` |
| Player smoke | exit code 0；marker=`TIMEKEY_PLAYER_SMOKE_PASS` | `player-smoke-summary.json` |
| 文案扫描 | 当前 Canvas 文本英文字母计数 `0` | Harness 运行时断言 |
| 字形覆盖 | Silver 覆盖当前集中式目录全部必需中文字符 | `CombatSceneAssetTests` |

## 字体与授权

`Silver.ttf` 来自本机已安装的 Poppy Works / Wolfgang Wozniak Silver 字体，副本 SHA-256 为 `7ADCF56D93142DED08FF18196F78FCAAF552411B9A94DC559DAC90EB5C91CEF1`。官方发布页列出简体中文支持和 CC BY 4.0 许可，并要求署名；总预算或总收入超过 10 万美元时需取得直接许可。项目内记录为 `Silver-ATTRIBUTION.txt`。

## 人工视觉检查

- `initial-1280x720.png`：标题、初始状态、36 格时间轴、七张卡和右侧详情均可读；没有方框字、裁切或控件重叠。
- `initial-1920x1080.png`：Silver 与中文卡面风格一致，详情区“第 03 格·第二行”和“结算时间轴”完整显示。
- `initial-2560x1080.png`：超宽布局保持居中，标题、时间轴、手牌和详情区均在画面内，没有拉伸或错位。
- `lighting-selected-1280x720.png`、`lighting-targeted-1280x720.png`、`lighting-preview-1920x1080.png`：选卡、锁定目标和合法时间轴提示完整，没有遮住棋盘或卡牌。
- `lighting-resolved-1920x1080.png`：时间轴显示“雷击”，HUD 显示结算完成与目标生命 0，均无缺字。
- `wind-clear-hit-1280x720.png`：清除预览、命中 1 个行动及绿色“命中”标记清晰，卡牌抬升态未遮挡详情区。

像素/亮度检查只用于拒绝空图；上述结论来自逐张打开八张实际 PNG 后的人工检查。

## 结论

当前 Unity 战斗切片所有已实现的玩家可见文本均已汉化并使用 Silver 字体。当前切片没有对白、配音、字幕、设置、教程或商店界面，因此没有对应资产；Godot 局外流程未被本阶段修改。
