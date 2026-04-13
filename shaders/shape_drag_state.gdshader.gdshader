shader_type canvas_item;

// 状态开关
uniform bool is_invalid = false; 

// 视觉参数 (全部导出供你在面板调整)
uniform vec4 gray_mask_color : source_color = vec4(0.3, 0.3, 0.3, 1.0);
uniform vec4 red_mask_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform float flash_speed = 15.0; // 闪烁速度
uniform float base_alpha = 0.8;   // 整体半透明度

void fragment() {
	vec4 col = texture(TEXTURE, UV);
	
	// 只对非透明的实体像素进行处理
	if (col.a > 0.0) {
		if (is_invalid) {
			// 非法状态：红色急促闪烁
			float flash = (sin(TIME * flash_speed) + 1.0) / 2.0;
			// 混合原始颜色与红色，闪烁强度在 0.4 到 0.9 之间变化
			col.rgb = mix(col.rgb, red_mask_color.rgb, mix(0.4, 0.9, flash));
			col.a *= base_alpha;
		} else {
			// 合法状态：静谧的灰色蒙版
			col.rgb = mix(col.rgb, gray_mask_color.rgb, 0.6);
			col.a *= base_alpha;
		}
	}
	COLOR = col;
}