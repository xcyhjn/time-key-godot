// 文件名: hourglass_shake.gdshader
// 功能: 沙漏图标震动效果
// 类型: CanvasItem Shader (2D)
shader_type canvas_item;

// 震动开关 (0=关闭, 1=开启)
uniform float shake_enabled : hint_range(0, 1) = 0.0;

// 震动强度 (0.0-1.0)
uniform float shake_intensity : hint_range(0.0, 1.0) = 0.5;

// 震动频率 (Hz)
uniform float shake_frequency : hint_range(0.1, 50.0) = 15.0;

// 随机种子
uniform float random_seed : hint_range(0.0, 1000.0) = 123.456;

// 颜色脉冲强度
uniform float color_pulse_strength : hint_range(0.0, 0.5) = 0.1;

// 简单伪随机函数
float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

void fragment() {
    // 原始UV
    vec2 original_uv = UV;

    // 震动偏移计算
    vec2 shake_offset = vec2(0.0);

    if (shake_enabled > 0.5) {
        float time = TIME * shake_frequency + random_seed * 0.01;

        // 多方向震动
        float x_offset = sin(time * 2.0 + original_uv.y * 3.0) * 0.5 +
                         cos(time * 1.7 + original_uv.x * 2.5) * 0.3;

        float y_offset = cos(time * 2.3 + original_uv.x * 2.8) * 0.5 +
                         sin(time * 1.9 + original_uv.y * 2.2) * 0.3;

        // 应用强度
        x_offset *= shake_intensity * 0.02;
        y_offset *= shake_intensity * 0.02;

        // 中心衰减
        vec2 center = vec2(0.5, 0.5);
        float distance_to_center = length(original_uv - center);
        float center_attenuation = 1.0 - smoothstep(0.0, 0.7, distance_to_center);

        shake_offset = vec2(x_offset, y_offset) * center_attenuation;
    }

    // 应用偏移
    vec2 shaken_uv = original_uv + shake_offset;

    // 采样纹理
    vec4 color = texture(TEXTURE, shaken_uv);

    // 颜色脉冲效果
    if (shake_enabled > 0.5 && color_pulse_strength > 0.01) {
        float pulse = sin(TIME * 8.0 + original_uv.x * 5.0) * 0.5 + 0.5;
        vec3 gold_tint = vec3(1.0, 0.843, 0.0);
        color.rgb = mix(color.rgb, gold_tint, pulse * color_pulse_strength);
    }

    COLOR = color;
}