shader_type canvas_item;

// ==========================================
// 文件名: enemy_intent_timeline_pulse.gdshader
// 用途:
// - 这是“敌人意图系统”中时间轴占位格专用的脉冲 shader。
// - 它单独挂在敌人时间轴格子的 Overlay 上，用来表示“这一整组格子属于同一个意图”。
//
// 接收什么数据:
// - pulse_color: 当前意图颜色（有效时红色，无效时灰色）
// - pulse_speed / min_alpha / max_alpha: 脉冲节奏
//
// 如何处理:
// - 对整块区域做轻微脉冲和波动透明度
// - 不负责方块缩放，缩放由 TimelineUI 的容器动画处理
//
// 数据传递到哪里:
// - 不传递，只直接输出到时间轴格子 Overlay
// ==========================================

uniform vec4 pulse_color : source_color = vec4(1.0, 0.25, 0.25, 1.0);
uniform float pulse_speed : hint_range(0.1, 10.0) = 3.0;
uniform float min_alpha : hint_range(0.0, 1.0) = 0.15;
uniform float max_alpha : hint_range(0.0, 1.0) = 0.75;

void fragment() {
	float wave = abs(sin((UV.x + UV.y) * 10.0 - TIME * pulse_speed));
	float alpha = mix(min_alpha, max_alpha, wave);

	COLOR.rgb = pulse_color.rgb;
	COLOR.a = alpha * pulse_color.a;
}
