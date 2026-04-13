shader_type canvas_item;
//卡牌选中高亮（已废弃）

uniform vec4 glow_color : source_color = vec4(1.0, 0.9, 0.2, 1.0); // 默认微黄色
uniform float glow_size : hint_range(0.0, 20.0, 1.0) = 8.0;        // 发光扩散的像素范围
uniform float glow_intensity : hint_range(0.0, 5.0, 0.1) = 1.5;    // 核心亮度乘区
uniform bool is_selected = false;                                  // 是否激活发光

void fragment() {
    vec4 col = texture(TEXTURE, UV);

    // 只有在激活状态，且当前像素是透明的（处于卡牌外部）时才计算发光
    if (is_selected && col.a == 0.0) {
        float max_alpha = 0.0;
        vec2 pixel_size = TEXTURE_PIXEL_SIZE;

        // 核心算法：圆形范围扫描，完美覆盖直角和圆角
        for(float x = -glow_size; x <= glow_size; x++) {
            for(float y = -glow_size; y <= glow_size; y++) {
                float dist = sqrt(x*x + y*y);
                // 只计算设定半径内的像素
                if (dist <= glow_size && dist > 0.0) {
                    vec4 sample_col = texture(TEXTURE, UV + vec2(x, y) * pixel_size);
                    // 如果扫描到了卡牌本体的像素
                    if (sample_col.a > 0.0) {
                        // 距离越远，权重（透明度）越低，形成渐弱质感
                        float weight = 1.0 - (dist / glow_size);
                        max_alpha = max(max_alpha, weight);
                    }
                }
            }
        }

        if (max_alpha > 0.0) {
            // 输出发光颜色，乘以强度和衰减透明度
            COLOR = vec4(glow_color.rgb * glow_intensity, max_alpha * glow_color.a);
        } else {
            COLOR = col;
        }
    } else {
        COLOR = col;
    }
}