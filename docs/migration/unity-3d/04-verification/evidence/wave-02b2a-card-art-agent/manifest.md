# Wave 02B2A 七卡原卡面 Manifest

> 生成日期：2026-08-01
> 范围：Godot 权威源、Unity Resources 原字节副本、Unity TextureImporter 静态设置、审查 contact sheet

## 逐卡清单

| stable ID | `front_image` | 源路径 | Unity 目标路径 | 尺寸 | 字节数 | SHA-256 | 导入结论 |
| --- | --- | --- | --- | --- | ---: | --- | --- |
| `lighting` | `lighting.png` | `card_asset/lighting.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/lighting.png` | 1135x1590 | 813176 | `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC` | 源/目标逐字节一致；现有 `.meta` 为 Default Texture（`textureType=0`、`spriteMode=0`），按 Prompt 只读未改，需主智能体统一导入确认 |
| `earthquake` | `earthquake.png` | `card_asset/earthquake.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/earthquake.png` | 1135x1590 | 921281 | `E03EC4E9DAA4DA9BB26F4EF80803B0CB9F506497C777560B80567F773C6463F1` | 源/目标逐字节一致；Single Sprite/UI、无 mipmap、Point、Clamp、Max Size 2048、无压缩 |
| `wind` | `wind.png` | `card_asset/wind.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/wind.png` | 1135x1590 | 947336 | `BFCDA4C0A872FE327EFDFB2FF6880E4A343CC176484CCE5BEE0BC13C39DDB498` | 源/目标逐字节一致；Single Sprite/UI、无 mipmap、Point、Clamp、Max Size 2048、无压缩 |
| `recover` | `recover.png` | `card_asset/recover.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/recover.png` | 1135x1590 | 933304 | `F9065BC4FD646D8EEFC463FBC35798EDDDDDE489C4214F6333ACCC6E54A23D99` | 源/目标逐字节一致；Single Sprite/UI、无 mipmap、Point、Clamp、Max Size 2048、无压缩 |
| `tower` | `tower_card.png` | `card_asset/tower_card.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/tower_card.png` | 1135x1590 | 848422 | `73BFFC6162D4D0D09D196835E3AC1BE5D40FEE19BC6C5BC391008386471AF05B` | 源/目标逐字节一致；Single Sprite/UI、无 mipmap、Point、Clamp、Max Size 2048、无压缩 |
| `poison` | `poison_card.png` | `card_asset/poison_card.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/poison_card.png` | 1135x1590 | 958097 | `936B2BE26879462D0A18E1F0F5867E3681461A744E73E56823E3B1E8AEDCB879` | 源/目标逐字节一致；Single Sprite/UI、无 mipmap、Point、Clamp、Max Size 2048、无压缩 |
| `tornado` | `tornado.png` | `card_asset/tornado.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/tornado.png` | 1135x1590 | 833376 | `B369DCC027670C4AA28E003FC61B97DCE5DB077B982AEE6BAFDC84766B00E564` | 源/目标逐字节一致；Single Sprite/UI、无 mipmap、Point、Clamp、Max Size 2048、无压缩 |

## 机器检查

- 七份 `card_data/*.json` 均通过 `ConvertFrom-Json` 读取；`name`、`front_image` 与冻结映射逐项一致。
- 七张目标 PNG 均为非空 PNG、`1135x1590`、24-bit RGB，纵横比统一为 `1135:1590`（约 `0.713836`）；无 alpha 通道。
- 七张源/目标的字节数和 SHA-256 均逐项一致；开始扫描与复制后二次扫描的源 SHA-256 也一致。
- 六份新增 `.meta` 静态校验结果均为 `textureType=8`、`spriteMode=1`、`enableMipMap=0`，Default/Standalone/WebGL 三个平台均为 `textureCompression=0`。
- `lighting.png.meta` 的既有静态值为 `textureType=0`、`spriteMode=0`；这是本 Agent 只读核验发现的集成待办，不修改已有资源。

## 视觉证据

- `seven-card-contact-sheet.png`：988x748，508590 bytes，SHA-256 `1D3EBEF8BBC4C8A0BCCAD34A62F555F32AC2631C2AFBBCA3CECD9ADF16B1853A`。
- contact sheet 读取 Unity Resources 目标文件，以精确 `1/5` 比例从 `1135x1590` 缩放为 `227x318`；未裁切，未改变宽高比。
- 人工检查确认七卡均完整显示四边、顶部 shape、中心插画、中文标题和描述；没有空白卡、截断卡框或可见拉伸。
- 所有卡面圆角外仍保留源图片中烘焙的棋盘格像素。该现象符合 MIG-014，不代表 alpha，也未被修图处理。

## 限制

- 本 Agent 未启动 Unity，以上“导入结论”对六张新增卡是 `.meta` 静态设置核验，不是 Unity Editor 实际导入/渲染结论。
- 公开发布授权仍未闭合，符合 MIG-005；本清单只证明本地迁移来源、字节和映射，不构成授权证明。
