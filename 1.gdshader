shader_type canvas_item;

// =====================
// 3D 卡片参数
// =====================
/** 是否启用3D效果 */
uniform bool use_3d = true;

/** 视场角度(FOV)，影响3D透视效果的强度 */
uniform float fov : hint_range(1, 179) = 90;

/** 是否剔除背面，开启可提高性能 */
uniform bool cull_back = true;

/** Y轴旋转角度 */
uniform float y_rot : hint_range(-180, 180) = 0.0;

/** X轴旋转角度 */
uniform float x_rot : hint_range(-180, 180) = 0.0;

/** 内缩程度，用于调整3D效果的强度 */
uniform float inset : hint_range(0, 1) = 0.0;

/** 悬停响应强度，影响鼠标交互的灵敏度 */
uniform float hovering = 1.0;

/** 2D平面旋转角度 */
uniform float rotation : hint_range(-10.0, 10.0, 0.1) = 0.0;

/** 最大交互力度，限制鼠标影响的程度 */
uniform float max_force : hint_range(0.1, 5.0, 0.1) = 1.0;

/** 全局鼠标位置，由脚本更新 */
global uniform vec2 mouse_screen_pos;

// =====================
// 闪光参数
// =====================
/** 是否启用闪光效果 */
uniform bool use_flash = true;

/** 闪光类型：
 * 0 = 普通白光
 * 1 = 金色
 * 2 = 彩虹
 */
uniform int flash_type : hint_range(0, 2) = 0;

/** 闪光是否跟随3D变换 */
uniform bool use_3d_flash = false;

/** 闪光遮罩贴图 */
uniform sampler2D mask_texture : filter_linear_mipmap;

/** 遮罩缩放比例 */
uniform float mask_scale : hint_range(0.1, 5.0) = 1.0;

/** 闪光移动速度 */
uniform float speed : hint_range(0.1, 5.0) = 1.0;

/** 条纹间距，值越大条纹越密集 */
uniform float stripe_spacing : hint_range(1.0, 50.0) = 20.0;

/** 条纹宽度，值越大条纹越窄 */
uniform float stripe_width : hint_range(1.0, 20.0) = 8.0;

/** 整体移动速度 */
uniform float move_speed : hint_range(0.1, 5.0) = 1.0;

/** 闪光强度 */
uniform float intensity : hint_range(0.0, 2.0) = 1.0;

/** 闪光角度（0-360度） */
uniform float angle : hint_range(0.0, 360.0) = 45.0;

/** 3D扫光最大移动范围 */
uniform float flash_max_move : hint_range(0.0, 2.0) = 1.0;

// =====================
// varyings
// =====================
varying flat vec2 o;
varying vec3 p;
varying vec2 transformed_world_to_canvas_coordinates;
varying flat vec2 pivot;
varying flat vec2 region_rate;
varying flat mat3 inv_rot_mat;
varying vec2 v_mouse_force;

// =====================
// 工具函数
// =====================
vec2 rotate(vec2 uv, vec2 pivot_r, float rot_angle) {
    float s = sin(rot_angle);
    float c = cos(rot_angle);
    mat2 rot = mat2(vec2(c, -s), vec2(s, c));
    return (rot * (uv - pivot_r)) + pivot_r;
}

// =====================
// 顶点变换
// =====================
void vertex() {
    vec2 my_region_rate = abs(VERTEX) * 2.0;
    region_rate = TEXTURE_PIXEL_SIZE * my_region_rate;

    if (VERTEX_ID == 0) pivot = UV;
    if (VERTEX_ID == 1) pivot = UV - vec2(0.0, region_rate.y);
    if (VERTEX_ID == 2) pivot = UV - vec2(region_rate.x, region_rate.y);
    if (VERTEX_ID == 3) pivot = UV - vec2(region_rate.x, 0.0);

    if (use_3d) {
        // 鼠标位置转换
        transformed_world_to_canvas_coordinates =
            (inverse(MODEL_MATRIX) * vec4(mouse_screen_pos, 0.0, 1.0)).xy;

        // 原始偏移并限位
        vec2 raw_force = (transformed_world_to_canvas_coordinates) /
                         length(region_rate / TEXTURE_PIXEL_SIZE);
        raw_force = clamp(raw_force, vec2(-max_force), vec2(max_force));

        // 应用强度
        vec2 mouse_force = hovering * 1.5 * raw_force;
        v_mouse_force = mouse_force; // 传递给fragment着色器

        float sin_b = sin(y_rot / 180.0 * PI + mouse_force.x);
        float cos_b = cos(y_rot / 180.0 * PI + mouse_force.x);
        float sin_c = sin(x_rot / 180.0 * PI - mouse_force.y);
        float cos_c = cos(x_rot / 180.0 * PI - mouse_force.y);

        inv_rot_mat[0][0] = cos_b;
        inv_rot_mat[0][1] = 0.0;
        inv_rot_mat[0][2] = -sin_b;

        inv_rot_mat[1][0] = sin_b * sin_c;
        inv_rot_mat[1][1] = cos_c;
        inv_rot_mat[1][2] = cos_b * sin_c;

        inv_rot_mat[2][0] = sin_b * cos_c;
        inv_rot_mat[2][1] = -sin_c;
        inv_rot_mat[2][2] = cos_b * cos_c;

        vec2 uv = (UV - pivot) / region_rate;
        float t = tan(fov / 360.0 * PI);

        VERTEX += (uv - 0.5) * my_region_rate * t * (1.0 - inset);
    }
}

// =====================
// 片段着色
// =====================
void fragment() {
    vec2 uv;
    vec4 tex;

    if (use_3d) {
        // 归一化局部 UV，用于透视与闪光计算
        vec2 local_uv = (UV - pivot) / region_rate;
        float t = tan(fov / 360.0 * PI);

        // 3D 透视
        p = inv_rot_mat * vec3((local_uv - 0.5), 0.5 / t);
        float v = (0.5 / t) + 0.5;
        p.xy *= v * inv_rot_mat[2].z;
        o = v * inv_rot_mat[2].xy;

        if (cull_back && p.z <= 0.0) discard;

        uv = (p.xy / p.z).xy - o;
        uv += 0.5;

        float asp = (region_rate / TEXTURE_PIXEL_SIZE).y /
                    (region_rate / TEXTURE_PIXEL_SIZE).x;
        uv.y *= asp;
        uv = rotate(uv, vec2(0.5, 0.5), rotation);
        uv.y /= asp;

        if (any(greaterThan(abs(uv - 0.5), vec2(0.5)))) discard;

        tex = texture(TEXTURE, pivot + uv * region_rate);
    } else {
        tex = texture(TEXTURE, UV);
        uv = UV;
    }

    vec3 color = tex.rgb;

    if (use_flash) {
        float t2;
        if (use_3d_flash && use_3d) {
            // 只使用鼠标X轴的移动来控制扫光
            float mouse_x_effect = clamp(v_mouse_force.x, -flash_max_move, flash_max_move);
            t2 = mouse_x_effect + 0.5; // 归一化到[0,1]范围，0.5为中心位置
        } else {
            t2 = TIME * move_speed;
        }

        // 计算扫光方向和遮罩
        float rad = radians(angle);
        vec2 dir = vec2(cos(rad), sin(rad));
        vec2 mask_uv = (use_3d ? uv : UV);
        mask_uv = (mask_uv - 0.5) * mask_scale + 0.5;
        float mask_alpha = texture(mask_texture, mask_uv).a;

        // 计算扫光强度
        float stripe = sin(dot(uv, dir) * stripe_spacing + t2 * PI * speed) * 0.5 + 0.5;
        float flash = pow(stripe, stripe_width) * intensity * mask_alpha;

        // 应用闪光效果
        if (flash_type == 0) {
            color += vec3(flash);
        } else if (flash_type == 1) {
            color += vec3(1.0, 0.85, 0.3) * flash;
        } else {
            float hue = fract(dot(uv, dir) + t2 * 0.2);
            vec3 rainbow = vec3(
                0.5 + 0.5 * sin(6.2831 * hue),
                0.5 + 0.5 * sin(6.2831 * (hue + 0.33)),
                0.5 + 0.5 * sin(6.2831 * (hue + 0.66))
            );
            color += rainbow * flash;
        }
    }

    COLOR = vec4(color, tex.a);
}