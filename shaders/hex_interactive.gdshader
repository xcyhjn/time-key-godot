//地块白色蒙版以及白色边框
shader_type canvas_item;

// 状态开关
uniform bool is_hovered = false;
uniform bool is_selected = false;
uniform bool is_valid_target = false;  // 是否是卡牌的有效目标地块
uniform bool is_invalid_target = false; // 是否是卡牌的无效目标地块

// =================================
// 导出参数：可供你随意调整的选中效果
// =================================
uniform vec4 select_line_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 白色描边
uniform float select_line_thickness : hint_range(0.0, 10.0) = 2.0;        // 描边粗细
uniform vec4 select_overlay_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // 蒙版颜色 (白色)
uniform float select_overlay_alpha : hint_range(0.0, 1.0) = 0.4;          // 蒙版透明度

// 悬浮提亮倍率（保留你之前的悬浮效果）
uniform float hover_brightness : hint_range(1.0, 2.0) = 1.2;

// =================================
// 新增：条件地块效果参数
// =================================
uniform vec4 valid_target_color : source_color = vec4(1.0, 1.0, 1.0, 0.6); // 有效目标地块的变白颜色
uniform vec4 invalid_target_color : source_color = vec4(0.5, 0.5, 0.5, 0.3); // 无效目标地块的灰色颜色
uniform float valid_target_brightness : hint_range(1.0, 2.0) = 1.5; // 有效目标的亮度增强

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    vec2 size = TEXTURE_PIXEL_SIZE * select_line_thickness;
    vec4 final_color = col;

    // 优先级：选中 > 条件地块效果 > 悬浮
    if (is_selected) {
        if (col.a == 0.0) {
            // 在透明区域检测周围像素，生成描边
            float alpha = 0.0;
            alpha += texture(TEXTURE, UV + vec2(size.x, 0.0)).a;
            alpha += texture(TEXTURE, UV + vec2(-size.x, 0.0)).a;
            alpha += texture(TEXTURE, UV + vec2(0.0, size.y)).a;
            alpha += texture(TEXTURE, UV + vec2(0.0, -size.y)).a;
            alpha += texture(TEXTURE, UV + vec2(size.x, size.y)).a;
            alpha += texture(TEXTURE, UV + vec2(-size.x, -size.y)).a;
            alpha += texture(TEXTURE, UV + vec2(size.x, -size.y)).a;
            alpha += texture(TEXTURE, UV + vec2(-size.x, size.y)).a;

            if (alpha > 0.0) {
                final_color = select_line_color;
            }
        } else {
            // 核心安全机制：在原有不透明像素上，混入白色蒙版（绝不影响遮挡）
            final_color.rgb = mix(col.rgb, select_overlay_color.rgb, select_overlay_alpha);
        }
    } else if (col.a > 0.0) {
        // 条件地块效果（仅在选择卡牌作用地块时播放）
        if (is_valid_target) {
            // 有效目标地块：变白效果 + 亮度增强
            final_color.rgb = mix(col.rgb, valid_target_color.rgb, valid_target_color.a);
            final_color.rgb *= valid_target_brightness;
        } else if (is_invalid_target) {
            // 无效目标地块：灰色"无效果"显示
            final_color.rgb = mix(col.rgb, invalid_target_color.rgb, invalid_target_color.a);
        } else if (is_hovered) {
            // 普通悬浮效果（其他时间播放）
            final_color.rgb *= hover_brightness;
        }
    }

    COLOR = final_color;
}