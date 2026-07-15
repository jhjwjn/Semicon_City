import bpy
import math
from mathutils import Vector


# Blender script
# Run in Blender: Scripting > Text > Open > Run Script
# Optional FBX output is controlled by EXPORT_FBX at the bottom.

PROCESS_NAMES = [
    "01_Oxidation",
    "02_Photolithography",
    "03_Etching",
    "04_Deposition",
    "05_Metallization",
    "06_Ion_Implant",
    "07_CMP",
    "08_Inspection",
]


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_mat(name, color, roughness=0.35, metallic=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        try:
            bsdf.inputs["Base Color"].default_value = color
            bsdf.inputs["Alpha"].default_value = alpha
            bsdf.inputs["Roughness"].default_value = roughness
            bsdf.inputs["Metallic"].default_value = metallic
        except Exception:
            pass

    mat.blend_method = "BLEND" if alpha < 1.0 else "OPAQUE"
    mat.use_screen_refraction = alpha < 1.0
    mat.show_transparent_back = True
    return mat


MAT_CONCRETE = None
MAT_DARK = None
MAT_METAL = None
MAT_GLASS = None
MAT_GLASS_DARK = None
MAT_LOUVER = None
MAT_LIGHT = None


def init_materials():
    global MAT_CONCRETE, MAT_DARK, MAT_METAL, MAT_GLASS, MAT_GLASS_DARK
    global MAT_LOUVER, MAT_LIGHT

    MAT_CONCRETE = make_mat(
        "LAB_Concrete_WarmGray",
        (0.62, 0.60, 0.55, 1.0),
        roughness=0.72,
    )
    MAT_DARK = make_mat(
        "LAB_Dark_Interior",
        (0.035, 0.045, 0.055, 1.0),
        roughness=0.55,
    )
    MAT_METAL = make_mat(
        "LAB_Brushed_Metal",
        (0.76, 0.77, 0.76, 1.0),
        roughness=0.25,
        metallic=0.35,
    )
    MAT_GLASS = make_mat(
        "LAB_Blue_Reflective_Glass",
        (0.48, 0.78, 0.92, 0.46),
        roughness=0.08,
        metallic=0.0,
        alpha=0.46,
    )
    MAT_GLASS_DARK = make_mat(
        "LAB_Dark_Blue_Glass",
        (0.08, 0.28, 0.38, 0.58),
        roughness=0.12,
        alpha=0.58,
    )
    MAT_LOUVER = make_mat(
        "LAB_White_Aluminum_Louver",
        (0.88, 0.88, 0.84, 1.0),
        roughness=0.32,
        metallic=0.15,
    )
    MAT_LIGHT = make_mat(
        "LAB_Warm_Light",
        (1.0, 0.72, 0.36, 1.0),
        roughness=0.25,
    )


def cube_obj(name, loc, scale, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        obj.data.materials.append(mat)
    return obj


def add_bevel(obj, amount=0.03, segments=1):
    modifier = obj.modifiers.new("Softened_Edges", "BEVEL")
    modifier.width = amount
    modifier.segments = segments
    modifier.affect = "EDGES"

    weighted = obj.modifiers.new("Weighted_Normals", "WEIGHTED_NORMAL")
    weighted.keep_sharp = True


def add_panel_grid(parent, origin, width, height, floors, cols, glass_mat, variant):
    x0, y, z0 = origin
    panel_w = width / cols
    floor_h = height / floors

    for floor in range(floors):
        for col in range(cols):
            use_dark = (col + floor + variant) % 7 == 0
            mat = MAT_GLASS_DARK if use_dark else glass_mat

            px = x0 - width * 0.5 + panel_w * (col + 0.5)
            pz = z0 + floor_h * (floor + 0.5)

            panel = cube_obj(
                f"{parent.name}_Glass_F{floor:02d}_C{col:02d}",
                (px, y, pz),
                (panel_w * 0.92, 0.08, floor_h * 0.88),
                mat,
            )
            panel.parent = parent

    mullion_thick = 0.055

    for col in range(cols + 1):
        px = x0 - width * 0.5 + panel_w * col
        bar = cube_obj(
            f"{parent.name}_Mullion_V_{col:02d}",
            (px, y - 0.055, z0 + height * 0.5),
            (mullion_thick, 0.11, height),
            MAT_METAL,
        )
        bar.parent = parent

    for floor in range(floors + 1):
        pz = z0 + floor_h * floor
        bar = cube_obj(
            f"{parent.name}_Mullion_H_{floor:02d}",
            (x0, y - 0.055, pz),
            (width, 0.11, mullion_thick),
            MAT_METAL,
        )
        bar.parent = parent


def add_vertical_louvers(parent, origin, width, height, count, variant):
    x0, y, z0 = origin
    spacing = width / count

    for i in range(count):
        if (i + variant) % 5 == 1:
            continue

        px = x0 - width * 0.5 + spacing * (i + 0.5)
        depth = 0.12 + 0.03 * ((i + variant) % 3)
        louver = cube_obj(
            f"{parent.name}_Vertical_Louver_{i:02d}",
            (px, y, z0 + height * 0.5),
            (0.055, depth, height * 0.92),
            MAT_LOUVER,
        )
        louver.parent = parent


def add_roof(parent, x, y, z, width, depth, overhang=0.35):
    roof = cube_obj(
        f"{parent.name}_Thin_Roof_Canopy",
        (x, y, z),
        (width + overhang * 2, depth + overhang * 2, 0.16),
        MAT_DARK,
    )
    roof.parent = parent
    add_bevel(roof, 0.025, 1)


def add_stairs(parent, x, y, z, width, step_count, direction=1):
    step_depth = 0.28
    step_height = 0.09

    for i in range(step_count):
        step = cube_obj(
            f"{parent.name}_Entry_Stair_{i:02d}",
            (
                x,
                y + direction * step_depth * i,
                z + step_height * i,
            ),
            (
                width,
                step_depth,
                step_height,
            ),
            MAT_CONCRETE,
        )
        step.parent = parent

    rail_left = cube_obj(
        f"{parent.name}_Stair_Rail_L",
        (x - width * 0.48, y + direction * step_depth * step_count * 0.5, z + 0.55),
        (0.04, step_depth * step_count, 0.08),
        MAT_METAL,
    )
    rail_right = cube_obj(
        f"{parent.name}_Stair_Rail_R",
        (x + width * 0.48, y + direction * step_depth * step_count * 0.5, z + 0.55),
        (0.04, step_depth * step_count, 0.08),
        MAT_METAL,
    )
    rail_left.parent = parent
    rail_right.parent = parent


def add_entrance(parent, x, y, z, width, height):
    frame = cube_obj(
        f"{parent.name}_Entrance_Dark_Recess",
        (x, y - 0.03, z + height * 0.5),
        (width, 0.18, height),
        MAT_DARK,
    )
    frame.parent = parent

    door_l = cube_obj(
        f"{parent.name}_Glass_Door_L",
        (x - width * 0.18, y - 0.12, z + height * 0.46),
        (width * 0.32, 0.06, height * 0.76),
        MAT_GLASS_DARK,
    )
    door_r = cube_obj(
        f"{parent.name}_Glass_Door_R",
        (x + width * 0.18, y - 0.12, z + height * 0.46),
        (width * 0.32, 0.06, height * 0.76),
        MAT_GLASS_DARK,
    )
    door_l.parent = parent
    door_r.parent = parent


def add_process_mark(parent, process_index, x, y, z):
    # Simple low-poly icon bars, not text, so Unity import stays robust.
    bar_count = 3 + (process_index % 4)
    for i in range(bar_count):
        h = 0.25 + 0.12 * ((i + process_index) % 3)
        bar = cube_obj(
            f"{parent.name}_Process_Mark_{i:02d}",
            (x - 0.35 + i * 0.18, y, z + h * 0.5),
            (0.08, 0.05, h),
            MAT_LIGHT,
        )
        bar.parent = parent


def create_lab_building(index, loc):
    name = f"Semicon_Lab_{PROCESS_NAMES[index]}"
    parent = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(parent)
    parent.location = loc

    width = 8.0
    depth = 5.0
    floors = 3
    floor_h = 1.15
    podium_h = 0.75
    tower_h = floors * floor_h
    variant = index

    # Concrete podium and rear structural core. These are behind glass,
    # so glass never overlaps the same plane as opaque walls.
    podium = cube_obj(
        f"{name}_Concrete_Podium",
        (loc[0], loc[1], loc[2] + podium_h * 0.5),
        (width + 0.3, depth, podium_h),
        MAT_CONCRETE,
    )
    podium.parent = parent
    add_bevel(podium, 0.035, 1)

    rear_core = cube_obj(
        f"{name}_Opaque_Rear_Service_Core",
        (loc[0], loc[1] + depth * 0.23, loc[2] + podium_h + tower_h * 0.5),
        (width * 0.94, depth * 0.42, tower_h),
        MAT_DARK,
    )
    rear_core.parent = parent

    side_core_w = 0.9 + 0.15 * (variant % 2)
    side_core = cube_obj(
        f"{name}_Concrete_Side_Core",
        (loc[0] - width * 0.5 + side_core_w * 0.5, loc[1] - depth * 0.08, loc[2] + podium_h + tower_h * 0.5),
        (side_core_w, depth * 0.78, tower_h),
        MAT_CONCRETE,
    )
    side_core.parent = parent
    add_bevel(side_core, 0.025, 1)

    # Front curtain wall.
    front_y = loc[1] - depth * 0.5 - 0.05
    add_panel_grid(
        parent,
        (loc[0], front_y, loc[2] + podium_h),
        width,
        tower_h,
        floors,
        9 + (variant % 2),
        MAT_GLASS,
        variant,
    )
    add_vertical_louvers(
        parent,
        (loc[0], front_y - 0.08, loc[2] + podium_h),
        width,
        tower_h,
        13,
        variant,
    )

    # Side glass strips.
    side_x = loc[0] + width * 0.5 + 0.04
    for col in range(3):
        strip = cube_obj(
            f"{name}_Side_Glass_Strip_{col:02d}",
            (
                side_x,
                loc[1] - depth * 0.28 + col * depth * 0.23,
                loc[2] + podium_h + tower_h * 0.5,
            ),
            (0.07, depth * 0.16, tower_h * 0.92),
            MAT_GLASS_DARK if col == variant % 3 else MAT_GLASS,
        )
        strip.parent = parent

    add_roof(
        parent,
        loc[0],
        loc[1],
        loc[2] + podium_h + tower_h + 0.12,
        width,
        depth,
    )

    canopy_y = front_y - 0.42
    canopy = cube_obj(
        f"{name}_Entry_Canopy",
        (loc[0] + 0.45 * ((variant % 3) - 1), canopy_y, loc[2] + podium_h + 0.08),
        (2.4, 1.0, 0.12),
        MAT_DARK,
    )
    canopy.parent = parent

    entrance_x = loc[0] + 0.45 * ((variant % 3) - 1)
    add_entrance(parent, entrance_x, front_y - 0.09, loc[2] + 0.05, 1.65, 0.62)
    add_stairs(parent, entrance_x, front_y - 1.15, loc[2] + 0.04, 2.3, 5, direction=-1)
    add_process_mark(parent, index, loc[0] + width * 0.37, front_y - 0.16, loc[2] + podium_h + 0.22)

    # Small roof mechanical volumes for research-building character.
    mech_count = 2 + (variant % 3)
    for i in range(mech_count):
        mech = cube_obj(
            f"{name}_Roof_Mechanical_{i:02d}",
            (
                loc[0] - width * 0.28 + i * 0.85,
                loc[1] + depth * 0.18,
                loc[2] + podium_h + tower_h + 0.36,
            ),
            (0.55, 0.8, 0.32),
            MAT_METAL,
        )
        mech.parent = parent
        add_bevel(mech, 0.025, 1)

    return parent


def add_lighting_and_camera():
    bpy.ops.object.light_add(type="SUN", location=(0, -18, 14))
    sun = bpy.context.object
    sun.name = "Sun_Key"
    sun.rotation_euler = (math.radians(45), 0, math.radians(25))
    sun.data.energy = 2.5

    bpy.ops.object.light_add(type="AREA", location=(20, -14, 8))
    area = bpy.context.object
    area.name = "Large_Softbox"
    area.data.energy = 350
    area.data.size = 9

    bpy.ops.object.camera_add(location=(18, -24, 10), rotation=(math.radians(63), 0, math.radians(38)))
    bpy.context.scene.camera = bpy.context.object


def export_fbx(path):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.name.startswith("Semicon_Lab_"):
            obj.select_set(True)
            for child in obj.children_recursive:
                child.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"EMPTY", "MESH"},
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
    )


def main():
    clear_scene()
    init_materials()

    spacing_x = 10.5
    spacing_z = 8.0

    for i in range(8):
        row = i // 4
        col = i % 4
        loc = (
            (col - 1.5) * spacing_x,
            row * spacing_z,
            0.0,
        )
        create_lab_building(i, loc)

    add_lighting_and_camera()

    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 64
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"


main()


EXPORT_FBX = False
FBX_PATH = "/Users/jinyeong/Desktop/SemiconCity/Assets/Models/Semicon_Lab_8Buildings.fbx"

if EXPORT_FBX:
    export_fbx(FBX_PATH)
