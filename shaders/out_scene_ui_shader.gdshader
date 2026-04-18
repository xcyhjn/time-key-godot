shader_type canvas_item;

// 唯一需要从外部接收的开关
uniform bool is_hovered = false;

// ==========================================
// ★ 悬浮与呼吸参数 (可在 Inspector 实时调整)
// ==========================================
uniform float float_height = -8.0;      // 悬浮的基础高度 (负数向上)
uniform float breathe_speed = 4.0;      // 呼吸的速率 (决定起伏快慢)
uniform float breathe_amplitude = 2.0;  // 呼吸的幅度 (决定起伏多高)
uniform vec4 highlight_color : source_color = vec4(1.2, 1.2, 1.2, 1.0); // 悬浮提亮倍率

void vertex() {
	if (is_hovered) {
		// 完全在 Shader 内部利用内置时间(TIME)自动计算呼吸起伏
		float wiggle = sin(TIME * breathe_speed) * breathe_amplitude;
		
		// 直接偏移顶点的 Y 轴：基础悬浮高度 + 呼吸波浪
		VERTEX.y += float_height + wiggle;
	}
}

void fragment() {
	// ★ 核心修复：UI 节点（特别是 Theme 渲染的节点）必须直接读取 COLOR。
	// 这里的 COLOR 已经包含了 Godot 处理好的背景贴图、九宫格变形以及按钮文字。
	vec4 base_color = COLOR;
	
	if (is_hovered) {
		// 仅提亮 RGB 通道，保持原本的透明度 Alpha 不变
		base_color.rgb *= highlight_color.rgb;
	}
	
	COLOR = base_color;
}