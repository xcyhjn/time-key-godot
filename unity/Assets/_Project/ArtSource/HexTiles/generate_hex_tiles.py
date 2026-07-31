"""Generate low-poly hex tile source files, FBX models, and render evidence.

Run from the repository root with:
    D:\\blender\\blender.exe --factory-startup --background --python \
      unity/Assets/_Project/ArtSource/HexTiles/generate_hex_tiles.py
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


RADIUS = 0.94
HEIGHT = 0.32
SIDES = 6
SCRIPT_VERSION = "1.0"

SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[5]
SOURCE_DIR = SCRIPT_PATH.parent
MODEL_DIR = PROJECT_ROOT / "unity/Assets/_Project/Resources/Art/Battle/Models"
EVIDENCE_DIR = PROJECT_ROOT / "docs/migration/unity-3d/04-verification/evidence/hex-tile-agent"


def reset_blend() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(collection):
            collection.remove(datablock)


def configure_units() -> None:
    scene = bpy.context.scene
    bpy.context.preferences.filepaths.save_version = 0
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"


def make_material(name: str, color: tuple[float, float, float, float], roughness: float = 0.86) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = next(
        node for node in material.node_tree.nodes if node.bl_idname == "ShaderNodeBsdfPrincipled"
    )
    principled.inputs["Base Color"].default_value = color
    principled.inputs["Roughness"].default_value = roughness
    principled.inputs["Metallic"].default_value = 0.0
    return material


def create_materials(variant: str) -> list[bpy.types.Material]:
    if variant == "grass":
        return [
            make_material("HT_GrassTop", (0.18, 0.40, 0.04, 1.0)),
            make_material("HT_GrassEdge", (0.012, 0.14, 0.02, 1.0)),
            make_material("HT_RedSoil", (0.16, 0.03, 0.025, 1.0)),
            make_material("HT_DarkBase", (0.008, 0.015, 0.025, 1.0), 0.78),
        ]
    if variant == "dirt":
        return [
            make_material("HT_DirtTop", (0.16, 0.035, 0.03, 1.0)),
            make_material("HT_DirtSide", (0.10, 0.02, 0.018, 1.0)),
            make_material("HT_DarkBase", (0.008, 0.015, 0.025, 1.0), 0.78),
        ]
    raise ValueError(f"Unknown variant: {variant}")


def ring_points(radius: float, z: float) -> list[tuple[float, float, float]]:
    return [
        (
            radius * math.cos(math.radians(index * 60.0)),
            radius * math.sin(math.radians(index * 60.0)),
            z,
        )
        for index in range(SIDES)
    ]


def build_hex_tile(name: str, variant: str) -> bpy.types.Object:
    materials = create_materials(variant)
    rings = [
        (0.000, 0.900),
        (0.025, RADIUS),
        (0.070, RADIUS),
        (0.085, 0.900),
        (0.235, 0.900),
        (0.255, 0.915),
        (0.285, 0.900),
        (HEIGHT, 0.900),
    ]

    vertices: list[tuple[float, float, float]] = []
    ring_indices: list[list[int]] = []
    for z, radius in rings:
        indices = []
        for point in ring_points(radius, z):
            indices.append(len(vertices))
            vertices.append(point)
        ring_indices.append(indices)

    bottom_center = len(vertices)
    vertices.append((0.0, 0.0, 0.0))
    inner_ring = []
    middle_ring = []
    accent_center = Vector((0.15, -0.10, 0.0))
    for radius, target in ((0.08, inner_ring), (0.32, middle_ring)):
        for point in ring_points(radius, HEIGHT):
            if target is inner_ring:
                point = (point[0] + accent_center.x, point[1] + accent_center.y, point[2])
            target.append(len(vertices))
            vertices.append(point)
    top_center = len(vertices)
    vertices.append((accent_center.x, accent_center.y, HEIGHT))

    faces: list[tuple[int, ...]] = []
    material_indices: list[int] = []
    base_index = 3 if variant == "grass" else 2
    soil_index = 2 if variant == "grass" else 1
    edge_index = 1 if variant == "grass" else soil_index
    side_materials = [base_index, base_index, base_index, soil_index, edge_index, edge_index, edge_index]

    for ring_index in range(len(ring_indices) - 1):
        lower = ring_indices[ring_index]
        upper = ring_indices[ring_index + 1]
        for index in range(SIDES):
            next_index = (index + 1) % SIDES
            faces.append((lower[index], lower[next_index], upper[next_index], upper[index]))
            material_indices.append(side_materials[ring_index])

    bottom = ring_indices[0]
    for index in range(SIDES):
        next_index = (index + 1) % SIDES
        faces.append((bottom_center, bottom[next_index], bottom[index]))
        material_indices.append(base_index)

    top = ring_indices[-1]
    accent_sectors = {0, 2, 4}
    for index in range(SIDES):
        next_index = (index + 1) % SIDES
        faces.append((top_center, inner_ring[index], inner_ring[next_index]))
        if index in accent_sectors:
            material_indices.append(edge_index)
        else:
            material_indices.append(0)
        faces.append((inner_ring[index], middle_ring[index], middle_ring[next_index], inner_ring[next_index]))
        material_indices.append(0)
        faces.append((middle_ring[index], top[index], top[next_index], middle_ring[next_index]))
        material_indices.append(0)

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.validate(verbose=True)
    mesh.update(calc_edges=True)
    for material in materials:
        mesh.materials.append(material)
    for polygon, material_index in zip(mesh.polygons, material_indices):
        polygon.material_index = material_index
        polygon.use_smooth = False

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    obj["hex_layout"] = "flat-top"
    obj["circumradius_m"] = RADIUS
    obj["layer_height_m"] = HEIGHT
    obj["origin_contract"] = "bottom-center"
    return obj


def object_bounds(obj: bpy.types.Object) -> dict[str, list[float]]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = [min(point[axis] for point in points) for axis in range(3)]
    maximum = [max(point[axis] for point in points) for axis in range(3)]
    dimensions = [maximum[axis] - minimum[axis] for axis in range(3)]
    return {
        "min_xyz_m": [round(value, 6) for value in minimum],
        "max_xyz_m": [round(value, 6) for value in maximum],
        "dimensions_xyz_m": [round(value, 6) for value in dimensions],
    }


def mesh_statistics(obj: bpy.types.Object) -> dict[str, object]:
    mesh = obj.data
    mesh.calc_loop_triangles()
    edge_use = {edge.index: 0 for edge in mesh.edges}
    for polygon in mesh.polygons:
        for edge_index in polygon.edge_keys:
            matching = next(
                edge.index
                for edge in mesh.edges
                if tuple(sorted(edge.vertices)) == tuple(sorted(edge_index))
            )
            edge_use[matching] += 1

    bm = bmesh.new()
    bm.from_mesh(mesh)
    signed_volume = bm.calc_volume(signed=True)
    bm.free()
    return {
        "vertices": len(mesh.vertices),
        "edges": len(mesh.edges),
        "polygons": len(mesh.polygons),
        "triangles": len(mesh.loop_triangles),
        "materials": len(mesh.materials),
        "material_names": [material.name for material in mesh.materials],
        "bounds_blender_xyz": object_bounds(obj),
        "origin_xyz_m": [round(value, 6) for value in obj.location],
        "signed_volume_m3": round(signed_volume, 6),
        "non_manifold_edge_count": sum(1 for count in edge_use.values() if count != 2),
        "flat_shaded": all(not polygon.use_smooth for polygon in mesh.polygons),
    }


def export_fbx(obj: bpy.types.Object, path: Path) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        path_mode="AUTO",
        embed_textures=False,
    )


def inspect_fbx_roundtrip(path: Path) -> dict[str, object]:
    reset_blend()
    configure_units()
    bpy.ops.import_scene.fbx(filepath=str(path), automatic_bone_orientation=False)
    imported = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    imported.data.calc_loop_triangles()
    return {
        "object_name": imported.name,
        "vertices": len(imported.data.vertices),
        "triangles": len(imported.data.loop_triangles),
        "materials": len(imported.data.materials),
        "bounds_blender_xyz": object_bounds(imported),
        "object_location_xyz_m": [round(value, 6) for value in imported.location],
        "object_scale_xyz": [round(value, 6) for value in imported.scale],
    }


def set_render_settings() -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.render.resolution_percentage = 100
    scene.world.color = (0.008, 0.012, 0.02)
    world_nodes = scene.world.node_tree if scene.world and scene.world.use_nodes else None
    if scene.world:
        scene.world.use_nodes = True
        world_nodes = scene.world.node_tree
    if world_nodes:
        background = world_nodes.nodes.get("Background")
        background.inputs["Color"].default_value = (0.008, 0.012, 0.02, 1.0)
        background.inputs["Strength"].default_value = 0.34
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


def add_floor() -> None:
    bpy.ops.mesh.primitive_plane_add(size=30.0, location=(0.0, 0.0, -0.002))
    floor = bpy.context.object
    floor.name = "Evidence_Ground"
    floor.data.materials.append(make_material("Evidence_Ground_Mat", (0.035, 0.055, 0.075, 1.0), 0.92))


def add_area_light(name: str, location: tuple[float, float, float], energy: float, size: float, color: tuple[float, float, float]) -> None:
    data = bpy.data.lights.new(name, type="AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    light = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(light)
    light.location = location
    point_at(light, Vector((0.0, 0.0, 0.18)))


def point_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_camera(location: tuple[float, float, float], target: tuple[float, float, float], ortho_scale: float) -> None:
    camera_data = bpy.data.cameras.new("Evidence_Camera")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = ortho_scale
    camera_data.lens = 50.0
    camera = bpy.data.objects.new("Evidence_Camera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    point_at(camera, Vector(target))
    bpy.context.scene.camera = camera


def duplicate_for_render(source: bpy.types.Object, name: str, location: tuple[float, float, float]) -> bpy.types.Object:
    duplicate = source.copy()
    duplicate.data = source.data.copy()
    duplicate.name = name
    bpy.context.collection.objects.link(duplicate)
    duplicate.location = location
    duplicate.hide_render = False
    return duplicate


def prepare_evidence_scene() -> tuple[bpy.types.Object, bpy.types.Object]:
    reset_blend()
    configure_units()
    grass = build_hex_tile("HexTile_Grass_EvidenceSource", "grass")
    dirt = build_hex_tile("HexTile_Dirt_EvidenceSource", "dirt")
    grass.hide_render = True
    dirt.hide_render = True
    add_floor()
    add_area_light("Key_Light", (3.8, -4.2, 6.5), 900.0, 4.0, (1.0, 0.78, 0.62))
    add_area_light("Fill_Light", (-4.0, -1.0, 3.8), 550.0, 3.5, (0.48, 0.68, 1.0))
    add_area_light("Rim_Light", (0.0, 4.5, 5.2), 700.0, 3.0, (0.72, 0.86, 1.0))
    set_render_settings()
    return grass, dirt


def render_oblique(path: Path, camera_location: tuple[float, float, float], reverse: bool) -> None:
    grass, dirt = prepare_evidence_scene()
    left = dirt if reverse else grass
    right = grass if reverse else dirt
    duplicate_for_render(left, "Evidence_LeftTile", (-1.05, 0.0, 0.0))
    duplicate_for_render(right, "Evidence_RightTile", (1.05, 0.0, 0.0))
    add_camera(camera_location, (0.0, 0.0, 0.14), 3.7)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_stack(path: Path) -> None:
    grass, dirt = prepare_evidence_scene()
    lower = duplicate_for_render(dirt, "Evidence_Lower_Dirt", (0.0, 0.0, 0.0))
    upper = duplicate_for_render(grass, "Evidence_Upper_Grass", (0.0, 0.0, HEIGHT))
    lower["stack_layer"] = 0
    upper["stack_layer"] = 1
    upper["stack_offset_m"] = HEIGHT
    add_camera((3.1, -4.5, 2.7), (0.0, 0.0, HEIGHT), 2.25)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def generate_variant(name: str, variant: str, blend_name: str, fbx_name: str) -> dict[str, object]:
    reset_blend()
    configure_units()
    obj = build_hex_tile(name, variant)
    stats = mesh_statistics(obj)
    blend_path = SOURCE_DIR / blend_name
    fbx_path = MODEL_DIR / fbx_name
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)
    export_fbx(obj, fbx_path)
    stats["blend_file"] = str(blend_path.relative_to(PROJECT_ROOT)).replace("\\", "/")
    stats["fbx_file"] = str(fbx_path.relative_to(PROJECT_ROOT)).replace("\\", "/")
    stats["fbx_size_bytes"] = fbx_path.stat().st_size
    return stats


def main() -> None:
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    EVIDENCE_DIR.mkdir(parents=True, exist_ok=True)

    grass_stats = generate_variant("HexTile_Grass", "grass", "HexTile_Grass.blend", "HexTile_Grass.fbx")
    dirt_stats = generate_variant("HexTile_Dirt", "dirt", "HexTile_Dirt.blend", "HexTile_Dirt.fbx")

    grass_roundtrip = inspect_fbx_roundtrip(MODEL_DIR / "HexTile_Grass.fbx")
    dirt_roundtrip = inspect_fbx_roundtrip(MODEL_DIR / "HexTile_Dirt.fbx")

    render_oblique(EVIDENCE_DIR / "front-oblique.png", (4.2, -5.4, 3.4), reverse=False)
    render_oblique(EVIDENCE_DIR / "back-oblique.png", (-4.2, 5.4, 3.4), reverse=True)
    render_stack(EVIDENCE_DIR / "two-layer-stack.png")

    validation = {
        "generator_version": SCRIPT_VERSION,
        "blender_version": bpy.app.version_string,
        "geometry_contract": {
            "layout": "flat-top",
            "circumradius_m": RADIUS,
            "single_layer_height_m": HEIGHT,
            "origin": "bottom-center",
            "stack_positions_blender_z_m": [0.0, HEIGHT],
            "expected_blender_bounds_xyz_m": [1.88, round(math.sqrt(3.0) * RADIUS, 6), HEIGHT],
            "expected_unity_bounds_xyz_m": [1.88, HEIGHT, round(math.sqrt(3.0) * RADIUS, 6)],
        },
        "variants": {
            "grass": grass_stats,
            "dirt": dirt_stats,
        },
        "fbx_roundtrip": {
            "grass": grass_roundtrip,
            "dirt": dirt_roundtrip,
        },
        "export": {
            "format": "FBX binary",
            "global_scale": 1.0,
            "apply_unit_scale": True,
            "apply_scale_options": "FBX_SCALE_UNITS",
            "axis_forward": "-Z",
            "axis_up": "Y",
            "bake_space_transform": False,
            "mesh_smooth_type": "FACE",
            "textures_embedded": False,
        },
        "evidence": [
            "front-oblique.png",
            "back-oblique.png",
            "two-layer-stack.png",
        ],
    }
    with (EVIDENCE_DIR / "model-validation.json").open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(validation, handle, indent=2)
        handle.write("\n")

    print(json.dumps(validation, indent=2))


if __name__ == "__main__":
    main()
