shader_type canvas_item;

// 效果预览参数
uniform vec4 effect_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 默认白色（无效果）
uniform float effect_intensity : hint_range(0.0, 1.0) = 0.0; // 效果强度

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    
    // 只对非透明区域应用效果
    if (col.a > 0.0) {
        // 混合原始颜色和效果颜色
        vec4 final_color = mix(col, effect_color, effect_intensity);
        
        // 如果效果颜色是红色，可以增强红色通道
        if (effect_color.r > 0.8 && effect_color.g < 0.2 && effect_color.b < 0.2) {
            // 红色效果：增强红色，减弱其他颜色
            final_color.r = col.r * (1.0 + effect_intensity * 0.5);
            final_color.g = col.g * (1.0 - effect_intensity * 0.3);
            final_color.b = col.b * (1.0 - effect_intensity * 0.3);
        }
        
        COLOR = final_color;
    } else {
        COLOR = col;
    }
}