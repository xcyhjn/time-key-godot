// 功能: 教程聚焦蒙版 Shader，负责绘制全屏暗色遮罩、圆形透明焦点和圆环描边。
// 核心逻辑: 根据 focus_position 与 focus_radius 计算像素到焦点的距离，只在圆外保留遮罩透明度，圆环由距离场单独绘制，避免出现横纵轴延伸高亮。
shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform vec4 mask_color : source_color = vec4(0.0, 0.0, 0.0, 0.58);
uniform vec4 focus_color : source_color = vec4(1.0, 0.86, 0.35, 0.95);
uniform vec2 focus_position = vec2(0.0, 0.0);
uniform float focus_radius = 86.0;
uniform float focus_outline_width = 4.0;
uniform float pulse_speed = 2.4;
uniform float pulse_amount = 8.0;
uniform float edge_softness = 2.0;
uniform bool has_focus = false;

varying vec2 local_position;


void vertex() {
	local_position = VERTEX;
}


void fragment() {
	vec2 pixel_position = local_position;
	float mask_alpha = mask_color.a;
	float ring_alpha = 0.0;

	if (has_focus) {
		float pulse = sin(TIME * pulse_speed) * pulse_amount;
		float radius = max(focus_radius + pulse, 1.0);
		float dist_to_focus = distance(pixel_position, focus_position);

		mask_alpha *= smoothstep(radius - edge_softness, radius + edge_softness, dist_to_focus);

		float half_outline = max(focus_outline_width * 0.5, 0.5);
		float dist_to_ring = abs(dist_to_focus - radius);
		ring_alpha = 1.0 - smoothstep(half_outline, half_outline + edge_softness, dist_to_ring);
	}

	float focus_alpha = ring_alpha * focus_color.a;
	vec3 final_color = mix(mask_color.rgb, focus_color.rgb, focus_alpha);
	COLOR = vec4(final_color, max(mask_alpha, focus_alpha));
}
