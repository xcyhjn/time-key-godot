shader_type canvas_item;

uniform vec4 pulse_color : source_color = vec4(1.0, 0.0, 0.0, 1.0); // 默认红色
uniform float pulse_speed : hint_range(0.1, 10.0) = 3.0;
uniform float min_alpha : hint_range(0.0, 1.0) = 0.3;
uniform float max_alpha : hint_range(0.0, 1.0) = 0.8;

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    // 只对非透明区域进行渲染
    if (col.a > 0.0) {
        // 利用正弦波实现深浅呼吸变化
        float wave = (sin(TIME * pulse_speed) + 1.0) / 2.0; 
        float current_alpha = mix(min_alpha, max_alpha, wave);
        
        // 混合原始颜色与脉冲颜色
        COLOR.rgb = mix(col.rgb, pulse_color.rgb, current_alpha);
        COLOR.a = col.a;
    }
}