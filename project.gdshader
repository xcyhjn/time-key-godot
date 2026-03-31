shader_type canvas_item;

uniform bool is_highlighted = false;
uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float intensity : hint_range(0.0, 1.0) = 0.3;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    if (is_highlighted) {
        // 混合原始颜色与高光颜色
        vec3 highlighted_rgb = mix(tex.rgb, highlight_color.rgb, intensity);
        // 也可以加上简单的边缘增强或整体提亮
        highlighted_rgb += highlight_color.rgb * 0.1;
        COLOR = vec4(highlighted_rgb, tex.a);
    } else {
        COLOR = tex;
    }
}