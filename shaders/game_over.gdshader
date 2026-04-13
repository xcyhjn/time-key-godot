shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform float grayscale_amount : hint_range(0.0, 1.0) = 0.0; // 灰度比例
uniform float burn_intensity : hint_range(0.0, 1.0) = 0.0;  // 边缘烧焦强度

void fragment() {
    vec4 scene_color = texture(screen_texture, SCREEN_UV);
    
    // 1. 灰度算法
    float gray = dot(scene_color.rgb, vec3(0.299, 0.587, 0.114));
    vec3 gray_color = mix(scene_color.rgb, vec3(gray), grayscale_amount);
    
    // 2. 模拟底片烧焦/边缘侵蚀
    vec2 center_uv = (SCREEN_UV - 0.5) * 2.0;
    float dist = length(center_uv);
    // 随着强度增加，边缘黑暗区域向中心蔓延
    float edge_burn = smoothstep(1.2 - burn_intensity, 0.5 - burn_intensity, dist);
    
    vec3 final_color = gray_color * edge_burn;
    
    // 给边缘加一点死灰的微红
    if (edge_burn < 0.9) {
        final_color = mix(final_color, vec3(0.05, 0.02, 0.02), (1.0 - edge_burn) * burn_intensity);
    }
    
    COLOR = vec4(final_color, 1.0);
}