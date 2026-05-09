shader_type canvas_item;

// ==========================================
// 文件名: enemy_intent_timeline_pulse.gdshader
// 用途:
// - 这是“敌人意图系统”中时间轴占位格专用的脉冲 shader。
// - 它单独挂在敌人时间轴格子的 Overlay 上，用来表示“这一整组格子属于同一个意图”。
//
// 接收什么数据:
// - pulse_color: 当前意图颜色（有效时金色，无效时灰色）
// - pulse_speed / min_alpha / max_alpha: 脉冲节奏
// - stripe_color / stripe_*: 被 hover/选中的意图条纹高亮
//
// 如何处理:
// - 对整块区域做轻微脉冲和波动透明度，并叠加呼吸式斜条纹
// - 不负责方块缩放，缩放由 TimelineUI 的容器动画处理
//
// 数据传递到哪里:
// - 不传递，只直接输出到时间轴格子 Overlay
// ==========================================

uniform vec4 pulse_color : source_color = vec4(1.0, 0.78, 0.18, 1.0);
uniform float pulse_speed : hint_range(0.1, 10.0) = 3.0;
uniform float min_alpha : hint_range(0.0, 1.0) = 0.15;
uniform float max_alpha : hint_range(0.0, 1.0) = 0.75;
uniform vec4 stripe_color : source_color = vec4(1.0, 0.85, 0.12, 1.0);
uniform float stripe_speed : hint_range(0.1, 12.0) = 3.8;
uniform float stripe_density : hint_range(2.0, 40.0) = 12.0;
uniform float stripe_width : hint_range(0.02, 0.45) = 0.12;
uniform float stripe_strength : hint_range(0.0, 1.0) = 0.75;

void fragment() {
	float wave = abs(sin((UV.x + UV.y) * 10.0 - TIME * pulse_speed));
	float alpha = mix(min_alpha, max_alpha, wave);

	float stripe_phase = (UV.x + UV.y) * stripe_density - TIME * stripe_speed;
	float stripe_distance = abs(fract(stripe_phase) - 0.5);
	float stripe_mask = 1.0 - smoothstep(stripe_width, stripe_width + 0.08, stripe_distance);
	float stripe_breathe = 0.55 + 0.45 * abs(sin(TIME * stripe_speed * 0.75));
	float stripe_mix = stripe_mask * stripe_breathe * stripe_strength;

	COLOR.rgb = mix(pulse_color.rgb, stripe_color.rgb, stripe_mix * stripe_color.a);
	COLOR.a = clamp((alpha * pulse_color.a) + stripe_mix * 0.22, 0.0, 1.0);
}
