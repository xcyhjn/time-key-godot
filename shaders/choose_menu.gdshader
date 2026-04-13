shader_type canvas_item;

uniform float slant_degree : hint_range(0.0, 3.0) = 0.335;
uniform float left_slant_pos : hint_range(-1.0, 2.0) = -1.0;
uniform float right_slant_pos : hint_range(-1.0, 2.0) = -1.0;

// 左侧渐变颜色（由脚本动态传入）
uniform vec4 left_color_start : source_color = vec4(1.0, 0.8, 0.0, 1.0);
uniform vec4 left_color_end : source_color = vec4(1.0, 1.0, 0.4, 1.0);

// 右侧图像与透明度
uniform sampler2D right_texture : source_color;
uniform float right_texture_alpha : hint_range(0.0, 1.0) = 0.8;

void fragment() {
    // --- 左侧逻辑 ---
    float left_logic = UV.x + (UV.y * slant_degree);
    float left_mask = step(left_logic, left_slant_pos);
    
    // --- 右侧逻辑 ---
    float right_logic = (1.0 - UV.x) + ((1.0 - UV.y) * slant_degree);
    float right_mask = step(right_logic, right_slant_pos);
    
    // 计算左侧渐变
    float gradient_factor = clamp(left_logic / (left_slant_pos + 0.01), 0.0, 1.0);
    vec4 left_final_color = mix(left_color_start, left_color_end, gradient_factor);
    
    // 获取右侧贴图颜色
    vec4 tex_color = texture(right_texture, UV);
    tex_color.a *= right_texture_alpha;
    
    // 最终颜色合成
    vec4 final_color = vec4(0.0);
    float final_alpha = 0.0;
    
    if (left_mask > 0.5) {
        final_color = left_final_color;
        final_alpha = left_final_color.a;
    } else if (right_mask > 0.5) {
        final_color = tex_color;
        final_alpha = tex_color.a;
    }
    
    COLOR = vec4(final_color.rgb, final_alpha);
}