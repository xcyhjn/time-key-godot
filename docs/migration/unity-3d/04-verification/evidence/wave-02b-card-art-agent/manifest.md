# Wave 02B Agent 01 卡牌素材证据清单

> 状态：已验证
> 负责人：Wave 02B Agent 01
> 最后验证日期：2026-07-31
> 证据来源：源 PNG、目标 PNG、SHA-256、逐像素 alpha 扫描、完整画布视觉检查

## 字节与图像属性

| 资产 | 源路径 | Unity 目标路径 | 源/目标/证据 SHA-256 | 字节 | 像素尺寸 | Alpha |
| --- | --- | --- | --- | ---: | ---: | --- |
| `lighting` 卡面 | `card_asset/lighting.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/lighting.png` | `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC` | 813176 | 1135x1590 | 无；24-bit RGB，1,804,650 个像素全部不透明 |
| `behide` 牌背 | `image/behide.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/behide.png` | `3AFF3AE738AACB3F08DDEA3CD00D00C0960C1C87B49AF9F5D3EB50E28FED0E87` | 503383 | 474x679 | 无；24-bit RGB，321,846 个像素全部不透明 |

证据 PNG `lighting-full-frame.png` 与 `behide-full-frame.png` 分别是源文件的精确字节副本，不是 Unity 导入后的压缩纹理，也没有重编码、裁切、缩放或颜色修改。

## 视觉检查

- `lighting-full-frame.png`：完整显示木质卡框、雷击像素画、中文标题“雷击”、描述“范围伤害100”和四条画布边界，没有裁切。
- `behide-full-frame.png`：完整显示牌背外框与中央旋涡，没有裁切。
- `lighting.png` 圆角外侧的灰白棋盘格是已烘焙的 RGB 像素，不是透明区域；Unity 导入不能通过 `Alpha Is Transparency` 自动移除。

## Unity 导入建议

| 资产 | Texture Type | Sprite Mode | Filter Mode | Max Size | 其他 |
| --- | --- | --- | --- | ---: | --- |
| `lighting.png` | Sprite (2D and UI) | Single | Point | 2048 | Alpha Source=None，关闭 Mip Maps，Wrap Mode=Clamp，Compression=None；uGUI 保持原纵横比 |
| `behide.png` | Sprite (2D and UI) | Single | Point | 1024 | Alpha Source=None，关闭 Mip Maps，Wrap Mode=Clamp，Compression=None；牌背比例与 125x175 不同，禁止非等比拉伸 |

本 Agent 未启动 Unity。验收时工作区并发生成了 `Cards.meta`、`lighting.png.meta` 与 `behide.png.meta`；三者当前都只有 `fileFormatVersion` 和 GUID，没有 `TextureImporter` 设置，因此不能据此声称导入建议已应用。`Cards.meta` 位于独占目录边界旁，本 Agent 未修改；由主智能体在共享导入阶段审查并应用上述设置。
