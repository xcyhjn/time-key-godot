shader_type canvas_item;

// 暴露出来的变量，可以在右侧检查器直接调参数
uniform vec2 scroll_speed = vec2(-0.05, 0.02); 

void fragment() {
    // 根据系统运行时间 (TIME) 和速度，不断偏移贴图的 UV 坐标
    vec2 shifted_uv = UV + TIME * scroll_speed;
    
    // 渲染偏移后的画面
    COLOR = texture(TEXTURE, shifted_uv);
}