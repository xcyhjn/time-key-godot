shader_type canvas_item;

// 控制斜边位置和倾斜度
uniform float slant_offset : hint_range(-1.0, 1.0) = 0.5;
uniform float degree : hint_range(0.0, 3.0) = 0.335;
uniform vec4 menu_color : source_color = vec4(0.1, 0.1, 0.1, 1.0);

void fragment() {
    // UV.x + UV.y * degree 创造出斜切感
    float shape = step(UV.x + (UV.y * degree), slant_offset);
    
    // 只显示切出的形状，其余透明
    COLOR = vec4(menu_color.rgb, shape * menu_color.a);
}