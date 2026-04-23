shader_type canvas_item;

// ==========================================
// 文件名: enemy_intent_target_ripple.gdshader
// 用途:
// - 这是“敌人意图系统”中目标地块(target)专用的地图波纹 shader。
// - 它被单独挂在地图地块顶部的 TargetOverlay 上，用来表示“哪些格子会受到影响”。
//
// 接收什么数据:
// - 一张六边形描边贴图（hexagon_frame.png）
// - ripple_color: 波纹颜色
// - ripple_speed / ripple_density / min_alpha / max_alpha: 波纹节奏参数
//
// 如何处理:
// - 只渲染贴图的非透明区域
// - 沿着 UV 的中心向外计算动态波纹强度，让边框区域呈现红色脉冲
//
// 数据传递到哪里:
// - 不传递到别处，只直接输出到屏幕
// ==========================================

uniform vec4 ripple_color : source_color = vec4(1.0, 0.2, 0.2, 1.0);
uniform float ripple_speed : hint_range(0.1, 10.0) = 3.2;
uniform float ripple_density : hint_range(1.0, 40.0) = 20.0;
uniform float min_alpha : hint_range(0.0, 1.0) = 0.2;
uniform float max_alpha : hint_range(0.0, 1.0) = 0.95;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	if (tex.a <= 0.0) {
		discard;
	}

	vec2 centered = UV - vec2(0.5);
	float dist = length(centered);
	float wave = abs(sin((dist * ripple_density) - TIME * ripple_speed));
	float alpha = mix(min_alpha, max_alpha, wave);

	COLOR.rgb = ripple_color.rgb;
	COLOR.a = tex.a * alpha * ripple_color.a;
}
