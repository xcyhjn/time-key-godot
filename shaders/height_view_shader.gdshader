shader_type canvas_item;

// ==========================================
// ★ 高度视图专用shader参数
// ==========================================
instance uniform float click_highlight = 0.0;   // 点击高亮状态 (0=关闭, 1=开启)
instance uniform float click_highlight_width : hint_range(0.0, 10.0) = 4.0;  // 高亮边框粗细
instance uniform vec4 click_highlight_color : source_color = vec4(1.0, 1.0, 1.0, 0.3);  // 高亮边框颜色

// ==========================================
// ★ 原始hex.shader兼容参数（简化保留）
// ==========================================
instance uniform float dissolve_blend = 0.0;    // 湮灭进度 (0=正常, 1=完全消融)
instance uniform float block_idx = 0.0;         // 当前层级 (0为底座)
instance uniform float total_height = 1.0;      // 柱子总高度 (用于全局消融计算)

// ==========================================
// ★ 湮灭参数（从hex.shader继承，用于高度视图溶解效果）
// ==========================================
uniform float dissolve_block_size = 40.0; // 像素块大小 (越小块越大)
uniform vec4 dissolve_edge_color : source_color = vec4(2.0, 1.5, 0.5, 1.0); // 燃烧/消融边缘发光

void vertex() {
	// 高度视图不需要顶点动画，保留空实现
}

void fragment() {
	vec4 tex_color = texture(TEXTURE, UV);

	// ==========================================
	// 1. 像素化湮灭逻辑（从hex.shader继承，用于高度视图溶解效果）
	// ==========================================
	if (dissolve_blend > 0.0) {
		// 计算这个片段在整根柱子中的“绝对全局 Y 坐标” (0.0 为最顶端，1.0为最底端)
		// 注意：Godot中 UV.y=0是贴图顶部。block_idx=0是底座，block_idx=total_height-1是最顶端。
		float global_uv_y = (UV.y + (total_height - block_idx - 1.0)) / total_height;

		// 生成像素化 UV 块
		vec2 block_uv = floor(UV * dissolve_block_size) / dissolve_block_size;
		// 伪随机数算法 (根据像素块生成固定噪声)
		float noise = fract(sin(dot(block_uv, vec2(12.9898, 78.233))) * 43758.5453);

		// 从上到下消融: 阀值由 dissolve_blend 控制，加上噪声产生残缺感
		float threshold = dissolve_blend * 1.5; // 乘以1.5确保能扫过全局

		if (global_uv_y + noise * 0.3 < threshold) {
			discard; // 像素直接灰飞烟灭！
		} else if (global_uv_y + noise * 0.3 < threshold + 0.05) {
			// 消融边缘产生高亮燃烧感
			tex_color *= dissolve_edge_color;
		}
	}

	// ==========================================
	// 2. 点击高亮边框效果（高度视图核心功能）
	// ==========================================
	vec4 final_color = tex_color;

	if (click_highlight > 0.5) {
		// 检测透明像素（纹理边缘）
		if (tex_color.a == 0.0) {
			// 计算周围像素的alpha值来创建边框
			vec2 pixel_size = TEXTURE_PIXEL_SIZE * click_highlight_width;
			float outline_alpha = 0.0;

			// 检查8个方向的相邻像素
			outline_alpha += texture(TEXTURE, UV + vec2(pixel_size.x, 0.0)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(-pixel_size.x, 0.0)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(0.0, pixel_size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(0.0, -pixel_size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(pixel_size.x, pixel_size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(-pixel_size.x, -pixel_size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(pixel_size.x, -pixel_size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(-pixel_size.x, pixel_size.y)).a;

			// 如果相邻像素有非透明像素，则显示高亮边框
			if (outline_alpha > 0.0) {
				final_color = click_highlight_color;
				final_color.a = min(outline_alpha, 1.0) * click_highlight_color.a;
			}
		} else {
			// 对于非透明像素，添加轻微的颜色叠加
			final_color.rgb = mix(final_color.rgb, click_highlight_color.rgb, click_highlight_color.a * 0.3);
		}
	}

	COLOR = final_color;
}