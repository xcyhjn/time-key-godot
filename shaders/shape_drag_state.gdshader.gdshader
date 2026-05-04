shader_type canvas_item;

// 状态开关
uniform bool is_invalid = false; 
// 拖拽显示状态：
// 0 = 普通合法，使用灰色蒙版；
// 1 = 普通非法，使用红色闪烁；
// 2 = clear 类即时卡牌与已有时间轴方格重叠，使用绿色蒙版。
// 保留 is_invalid 是为了兼容旧代码；当 is_invalid 为 true 时仍然强制视为红色非法。
uniform int drag_visual_state = 0;

// 视觉参数 (全部导出供你在面板调整)
uniform vec4 gray_mask_color : source_color = vec4(0.3, 0.3, 0.3, 1.0);
uniform vec4 red_mask_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);
uniform vec4 clear_overlap_mask_color : source_color = vec4(0.2, 0.9, 0.35, 1.0);
uniform float flash_speed = 15.0; // 闪烁速度
uniform float base_alpha = 0.8;   // 整体半透明度

void fragment() {
	vec4 col = texture(TEXTURE, UV);
	
	// 只对非透明的实体像素进行处理
	if (col.a > 0.0) {
		int state = drag_visual_state;
		if (is_invalid) {
			state = 1;
		}
		if (state == 1) {
			// 非法状态：红色急促闪烁
			float flash = (sin(TIME * flash_speed) + 1.0) / 2.0;
			// 混合原始颜色与红色，闪烁强度在 0.4 到 0.9 之间变化
			col.rgb = mix(col.rgb, red_mask_color.rgb, mix(0.4, 0.9, flash));
			col.a *= base_alpha;
		} else if (state == 2) {
			// clear 重叠状态：绿色蒙版，表示这些时间轴占位会被即时清除。
			col.rgb = mix(col.rgb, clear_overlap_mask_color.rgb, 0.65);
			col.a *= base_alpha;
		} else {
			// 合法状态：静谧的灰色蒙版
			col.rgb = mix(col.rgb, gray_mask_color.rgb, 0.6);
			col.a *= base_alpha;
		}
	}
	COLOR = col;
}
