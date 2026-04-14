shader_type canvas_item;

// ==========================================
// ★ 状态控制开关 (由脚本传入)
// ==========================================
instance uniform float highlight_blend = 0.0;   // 悬浮进度 (0~1)
instance uniform float is_selected_blend = 0.0; // 选中进度 (0~1)
instance uniform float dissolve_blend = 0.0;    // 湮灭进度 (0=正常, 1=完全消融)

instance uniform float block_idx = 0.0;         // 当前层级 (接收高度索引以匹配地形起伏)
instance uniform float total_height = 1.0;      // 柱子总高度 (用于全局消融计算)

// ==========================================
// ★ 悬浮与选中参数 (与 hex.gdshader 保持绝对一致)
// ==========================================
uniform float max_float_height = -35.0;
uniform float wiggle_speed = 3.5;
uniform float wiggle_amount = 3.0;
uniform vec4 highlight_color : source_color = vec4(1.5, 1.5, 1.5, 1.0);

uniform vec4 select_overlay_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float select_overlay_alpha : hint_range(0.0, 1.0) = 0.4;

// ==========================================
// ★ 湮灭参数
// ==========================================
uniform float dissolve_block_size = 40.0; // 像素块大小
uniform vec4 dissolve_edge_color : source_color = vec4(2.0, 1.5, 0.5, 1.0);

// ★ 1. 顶点着色器：完美复刻地块的上下起伏
void vertex() {
    if (block_idx > 0.0 && highlight_blend > 0.0) {
        float target_height = max_float_height - (block_idx * 5.0); 
        float wiggle = sin(TIME * wiggle_speed + (block_idx * 1.5)) * wiggle_amount;
        VERTEX.y += (target_height + wiggle) * highlight_blend;
    }
}

// ★ 2. 片段着色器：复刻消融与高亮，并保护UI
void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // 【核心UI保护】：如果像素本身是完全透明的（例如进度条扣血空出的部分），直接丢弃，绝不渲染！
    if (tex_color.a == 0.0) {
        discard;
    }
    
    // 像素化湮灭逻辑
    if (dissolve_blend > 0.0) {
        float global_uv_y = (UV.y + (total_height - block_idx - 1.0)) / total_height;
        vec2 block_uv = floor(UV * dissolve_block_size) / dissolve_block_size;
        float noise = fract(sin(dot(block_uv, vec2(12.9898, 78.233))) * 43758.5453);
        float threshold = dissolve_blend * 1.5; 
        
        if (global_uv_y + noise * 0.3 < threshold) {
            discard; // 像素灰飞烟灭
        } else if (global_uv_y + noise * 0.3 < threshold + 0.05) {
            tex_color *= dissolve_edge_color; // 边缘发光
        }
    }
    
    // 悬浮与选中渲染
    vec4 final_color = mix(tex_color, tex_color * highlight_color, highlight_blend);
    
    if (is_selected_blend > 0.5) {
        // UI直接叠加蒙版即可，不需要复杂的像素偏移描边
        final_color.rgb = mix(final_color.rgb, select_overlay_color.rgb, select_overlay_alpha);
    }
    
    COLOR = final_color;
}