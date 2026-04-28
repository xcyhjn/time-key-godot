shader_type canvas_item;

// ==========================================
// ★ 状态机统一控制参数 (3D和平铺通用)
// ==========================================
instance uniform float highlight_blend = 0.0;   // 范围波及进度 (0~1)
instance uniform float is_selected_blend = 0.0; // 中心选中描边进度 (0~1)
instance uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// 局外收获常驻高亮层。
// settlement_highlight_blend 只负责“变白/变金”，不推动顶点上浮；
// settlement_hover_blend 才在鼠标悬浮时接入原本的 hex 悬浮位移动画。
instance uniform float settlement_highlight_blend = 0.0;
instance uniform float settlement_hover_blend = 0.0;
instance uniform vec4 settlement_highlight_color : source_color = vec4(1.0, 0.78, 0.18, 1.0);

instance uniform float dissolve_blend = 0.0;    // 毁灭/隐藏进度 (0=正常, 1=完全消融)
instance uniform float block_idx = 0.0;         // 当前层级 (0为底座)
instance uniform float total_height = 1.0;      // 柱子总高度

// ★ 核心开关：0.0为3D视图，1.0为平铺视图
instance uniform float is_flat_view = 0.0;      

uniform float max_float_height = -35.0;
uniform float wiggle_speed = 3.5;
uniform float wiggle_amount = 3.0;
uniform float highlight_width : hint_range(0.0, 10.0) = 4.0; // 描边粗细

uniform float dissolve_block_size = 40.0; 
uniform vec4 dissolve_edge_color : source_color = vec4(2.0, 1.5, 0.5, 1.0); 

void vertex() {
    bool can_float = (block_idx > 0.0) || (total_height <= 1.5);
    
    // ★ 只有在3D视图 (is_flat_view < 0.5) 时才允许上下浮动！
    float hover_strength = max(highlight_blend, settlement_hover_blend);
    if (is_flat_view < 0.5 && can_float && hover_strength > 0.0) {
        float target_height = max_float_height - (block_idx * 5.0); 
        float wiggle = sin(TIME * wiggle_speed + (block_idx * 1.5)) * wiggle_amount;
        VERTEX.y += (target_height + wiggle) * hover_strength;
    }
}

void fragment() {
    // ★ 核心修复：必须乘以 COLOR，否则代码里的 modulate.a = 0.0 彻底无效！
    vec4 tex_color = texture(TEXTURE, UV) * COLOR;
    vec4 final_color = tex_color;

    // 1. 消融逻辑 (用于平铺隐藏侧边，或地块彻底被毁)
    if (dissolve_blend > 0.0) {
        float global_uv_y = (UV.y + (total_height - block_idx - 1.0)) / total_height;
        vec2 block_uv = floor(UV * dissolve_block_size) / dissolve_block_size;
        float noise = fract(sin(dot(block_uv, vec2(12.9898, 78.233))) * 43758.5453);
        float threshold = dissolve_blend * 1.5; 
        
        if (global_uv_y + noise * 0.3 < threshold) {
            discard;
        } else if (global_uv_y + noise * 0.3 < threshold + 0.05) {
            final_color *= dissolve_edge_color;
        }
    }

    // 2. 状态机高亮逻辑 (3D 和 平铺 无缝共用)
    if (settlement_highlight_blend > 0.0 && tex_color.a > 0.0) {
        final_color.rgb = mix(
            final_color.rgb,
            settlement_highlight_color.rgb,
            settlement_highlight_blend * settlement_highlight_color.a
        );
    }

    if (is_selected_blend > 0.5) {
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
            final_color.rgb = mix(final_color.rgb, highlight_color.rgb, highlight_color.a * 0.3 * is_selected_blend);
        }
    } else if (highlight_blend > 0.0) {
        if (tex_color.a > 0.0) {
            final_color.rgb = mix(final_color.rgb, highlight_color.rgb, highlight_blend * 0.4);
        }
    }

    COLOR = final_color;
}
