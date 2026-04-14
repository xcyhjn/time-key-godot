shader_type canvas_item;

// ★ 接收 hex_map.gd 传来的动画变量 (必须用 instance uniform 才能独立控制)
instance uniform float dissolve_blend = 0.0;    // 湮灭进度 (0=正常, 1=完全消融)
instance uniform float click_highlight = 0.0;   // 点击高亮状态 (0=关闭, 1=开启)
instance uniform float click_highlight_width : hint_range(0.0, 10.0) = 4.0;
instance uniform vec4 click_highlight_color : source_color = vec4(1.0, 1.0, 1.0, 0.3);

// 湮灭效果参数
uniform float dissolve_block_size = 40.0;
uniform vec4 dissolve_edge_color : source_color = vec4(2.0, 1.5, 0.5, 1.0);

void fragment() {
    // 获取原生颜色
    vec4 col = texture(TEXTURE, UV);
    
    // ★ 核心保护机制：如果当前像素原本就是完全透明的（比如进度条被扣血的部分），直接丢弃，千万别处理！
    if (col.a == 0.0) {
        discard;
    }

    // 1. 像素化湮灭逻辑
    if (dissolve_blend > 0.0) {
        // UI 的 UV 比较简单，直接用自身的 UV 即可
        vec2 block_uv = floor(UV * dissolve_block_size) / dissolve_block_size;
        float noise = fract(sin(dot(block_uv, vec2(12.9898, 78.233))) * 43758.5453);

        float threshold = dissolve_blend * 1.5;

        if (UV.y + noise * 0.3 < threshold) {
            discard; // 彻底消融
        } else if (UV.y + noise * 0.3 < threshold + 0.05) {
            col *= dissolve_edge_color; // 边缘发光
        }
    }

    // 2. 点击高亮效果 (高度视图使用)
    if (click_highlight > 0.5) {
        // 为血条叠加一层高亮颜色
        col.rgb = mix(col.rgb, click_highlight_color.rgb, click_highlight_color.a * 0.8);
    }

    COLOR = col;
}