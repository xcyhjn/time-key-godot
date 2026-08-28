shader_type canvas_item;

// 这个变量将由 Tween 来控制
uniform float circle_size : hint_range(0.0, 1.0) = 1.0;
// 圆形边缘的平滑度，0.01 比较清脆
uniform float screen_width = 1152.0;
uniform float screen_height = 648.0;

void fragment() {
    // 获取屏幕比例，防止圆形变成椭圆
    float ratio = screen_width / screen_height;
    vec2 uv = UV;
    uv.x *= ratio;

    // 计算圆心（屏幕中心）坐标
    vec2 center = vec2(0.5 * ratio, 0.5);

    // 计算像素点到中心的距离
    float dist = distance(uv, center);

    // 核心逻辑：如果距离大于尺寸，则显示黑色；小于则透明
    // smoothstep 用于消除锯齿
    float mask = smoothstep(circle_size, circle_size + 0.01, dist);

    COLOR = vec4(0.0, 0.0, 0.0, mask);
}