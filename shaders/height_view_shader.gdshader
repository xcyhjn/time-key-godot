shader_type canvas_item;

// ==========================================
// ★ 统一接收状态机参数 (与3D视图Shader接口一致)
// ==========================================
instance uniform float highlight_blend = 0.0;   // 范围波及进度 (0~1)
instance uniform float is_selected_blend = 0.0; // 中心选中描边进度 (0~1)
instance uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 状态机传来的颜色

uniform float highlight_width : hint_range(0.0, 10.0) = 4.0; // 描边粗细

void vertex() {
    // 平铺视图不需要浮动，保留空实现
}

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    vec4 final_color = tex_color;

    // ==========================================
    // 状态机接管的高亮逻辑
    // ==========================================
    if (is_selected_blend > 0.5) {
        // 这是中心地块 (HOVER_TARGET_VALID) -> 画描边 + 内部染色
        if (tex_color.a == 0.0) {
            vec2 pixel_size = TEXTURE_PIXEL_SIZE * highlight_width;
            float outline_alpha = 0.0;

            outline_alpha += texture(TEXTURE, UV + vec2(pixel_size.x, 0.0)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(-pixel_size.x, 0.0)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(0.0, pixel_size.y)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(0.0, -pixel_size.y)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(pixel_size.x, pixel_size.y)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(-pixel_size.x, -pixel_size.y)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(pixel_size.x, -pixel_size.y)).a;
            outline_alpha += texture(TEXTURE, UV + vec2(-pixel_size.x, pixel_size.y)).a;

            if (outline_alpha > 0.0) {
                final_color = highlight_color;
                final_color.a = min(outline_alpha, 1.0) * highlight_color.a;
            }
        } else {
            // 内部染色叠加
            final_color.rgb = mix(final_color.rgb, highlight_color.rgb, highlight_color.a * 0.3 * is_selected_blend);
        }
    } else if (highlight_blend > 0.0) {
        // 这是被波及的边缘地块 (AOE_RANGE) -> 只有染色，无描边
        if (tex_color.a > 0.0) {
            final_color.rgb = mix(final_color.rgb, highlight_color.rgb, highlight_blend * 0.4);
        }
    }

    COLOR = final_color;
}