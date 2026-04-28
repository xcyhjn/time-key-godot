shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

uniform vec4 fog_tint : source_color = vec4(0.86, 0.92, 0.96, 0.34);
uniform float blur_strength : hint_range(0.0, 1.0) = 0.60;
uniform float blur_radius : hint_range(0.0, 8.0) = 2.8;
uniform float blur_lod : hint_range(0.0, 4.0) = 4.0;
uniform float fog_alpha : hint_range(0.0, 1.0) = 0.8;
uniform float fog_scale : hint_range(0.2, 8.0) = 4.0;
uniform float fog_speed : hint_range(0.0, 0.2) = 0.25;
uniform float fog_contrast : hint_range(0.1, 2.0) = 0.90;
uniform float distortion_pixels : hint_range(0.0, 10.0) = 3.0;

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	mat2 drift = mat2(vec2(1.6, 1.2), vec2(-1.2, 1.6));

	for (int i = 0; i < 5; i++) {
		value += amplitude * noise(p);
		p = drift * p + vec2(13.17, 7.31);
		amplitude *= 0.5;
	}

	return value;
}

vec3 soft_blur(vec2 uv, vec2 pixel) {
	vec3 color = textureLod(screen_texture, uv, blur_lod).rgb * 0.28;

	color += textureLod(screen_texture, uv + vec2(pixel.x, 0.0), blur_lod).rgb * 0.12;
	color += textureLod(screen_texture, uv - vec2(pixel.x, 0.0), blur_lod).rgb * 0.12;
	color += textureLod(screen_texture, uv + vec2(0.0, pixel.y), blur_lod).rgb * 0.12;
	color += textureLod(screen_texture, uv - vec2(0.0, pixel.y), blur_lod).rgb * 0.12;

	color += textureLod(screen_texture, uv + pixel, blur_lod).rgb * 0.06;
	color += textureLod(screen_texture, uv - pixel, blur_lod).rgb * 0.06;
	color += textureLod(screen_texture, uv + vec2(pixel.x, -pixel.y), blur_lod).rgb * 0.06;
	color += textureLod(screen_texture, uv + vec2(-pixel.x, pixel.y), blur_lod).rgb * 0.06;

	return color;
}

void fragment() {
	vec2 fog_uv = UV * fog_scale + vec2(TIME * fog_speed, -TIME * fog_speed * 0.58);
	float fog = fbm(fog_uv);
	fog = smoothstep(0.5 - fog_contrast * 0.18, 0.5 + fog_contrast * 0.18, fog);

	vec2 distortion = vec2(
		fbm(fog_uv + vec2(9.8, 2.1)) - 0.5,
		fbm(fog_uv + vec2(3.7, 8.4)) - 0.5
	) * SCREEN_PIXEL_SIZE * distortion_pixels;

	vec2 screen_uv = SCREEN_UV + distortion;
	vec3 scene_color = textureLod(screen_texture, screen_uv, 0.0).rgb;
	vec3 blurred_color = soft_blur(screen_uv, SCREEN_PIXEL_SIZE * blur_radius);

	float veil = fog_alpha * mix(0.45, 1.0, fog);
	vec3 softened = mix(scene_color, blurred_color, blur_strength);
	vec3 misted = mix(softened, fog_tint.rgb, fog_tint.a * veil);

	COLOR = vec4(misted, 1.0);
}
