shader_type canvas_item;

// ==========================================
// ★ 状态控制开关 (由脚本传入)
// ==========================================
instance uniform float highlight_blend = 0.0;   // 悬浮进度 (0~1)
instance uniform float is_selected_blend = 0.0; // 选中进度 (0~1)
instance uniform float dissolve_blend = 0.0;    // 湮灭进度 (0=正常, 1=完全消融)

instance uniform float block_idx = 0.0;         // 当前层级 (0为底座)
instance uniform float total_height = 1.0;      // 柱子总高度 (用于全局消融计算)

// ==========================================
// ★ 悬浮与选中参数
// ==========================================
uniform float max_float_height = -35.0;
uniform float wiggle_speed = 3.5;
uniform float wiggle_amount = 3.0;
uniform vec4 highlight_color : source_color = vec4(1.5, 1.5, 1.5, 1.0);

uniform vec4 select_line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float select_line_thickness : hint_range(0.0, 10.0) = 2.0;
uniform vec4 select_overlay_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float select_overlay_alpha : hint_range(0.0, 1.0) = 0.4;

// ==========================================
// ★ 湮灭参数 (可导出调整)
// ==========================================
uniform float dissolve_block_size = 40.0; // 像素块大小 (越小块越大)
uniform vec4 dissolve_edge_color : source_color = vec4(2.0, 1.5, 0.5, 1.0); // 燃烧/消融边缘发光

void vertex() {
	if (block_idx > 0.0 && highlight_blend > 0.0) {
		float target_height = max_float_height - (block_idx * 5.0); 
		float wiggle = sin(TIME * wiggle_speed + (block_idx * 1.5)) * wiggle_amount;
		VERTEX.y += (target_height + wiggle) * highlight_blend;
	}
}

void fragment() {
	vec4 tex_color = texture(TEXTURE, UV);
	
	// ==========================================
	// 1. 像素化湮灭逻辑 (最优先计算)
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
	// 2. 悬浮与选中渲染 (基于未被湮灭的像素)
	// ==========================================
	vec4 final_color = mix(tex_color, tex_color * highlight_color, highlight_blend);
	
	if (is_selected_blend > 0.5) {
		if (tex_color.a == 0.0) {
			vec2 size = TEXTURE_PIXEL_SIZE * select_line_thickness;
			float outline_alpha = 0.0;
			outline_alpha += texture(TEXTURE, UV + vec2(size.x, 0.0)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(-size.x, 0.0)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(0.0, size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(0.0, -size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(size.x, size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(-size.x, -size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(size.x, -size.y)).a;
			outline_alpha += texture(TEXTURE, UV + vec2(-size.x, size.y)).a;

			if (outline_alpha > 0.0) {
				final_color = select_line_color;
				final_color.a = min(outline_alpha, 1.0) * select_line_color.a; 
			}
		} else {
			final_color.rgb = mix(final_color.rgb, select_overlay_color.rgb, select_overlay_alpha);
		}
	}
	
	COLOR = final_color;
}