import bpy
import math
from mathutils import Vector


# Premium semiconductor research lab generator.
# Blender usage:
# 1. Open Blender.
# 2. Scripting > Text > Open this file.
# 3. Run Script.
#
# Material policy:
# This script intentionally does not assign final glass color or realistic Unity
# materials. It only creates clean material slots with semantic names so Unity
# materials can be assigned later without geometry conflicts.


EXPORT_FBX = False
FBX_PATH = "/Users/jinyeong/Desktop/SemiconCity/Assets/Models/Premium_Semicon_Lab.fbx"


COLLECTION_NAME = "Premium_Semicon_Lab"


MAT_STRUCTURAL = None
MAT_SECONDARY = None
MAT_PANEL = None
MAT_GLASS_PLACEHOLDER = None
MAT_DARK_PLACEHOLDER = None
MAT_METAL = None
MAT_RAIL = None
MAT_FLOOR = None
MAT_DETAIL = None
MAT_LIGHT_PLACEHOLDER = None


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_collection(name):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def link_to_collection(obj, collection):
    for c in obj.users_collection:
        c.objects.unlink(obj)
    collection.objects.link(obj)


def make_mat(name, color, roughness=0.5, metallic=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = color

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        input_names = bsdf.inputs.keys()
        if "Base Color" in input_names:
            bsdf.inputs["Base Color"].default_value = color
        if "Alpha" in input_names:
            bsdf.inputs["Alpha"].default_value = alpha
        if "Roughness" in input_names:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Metallic" in input_names:
            bsdf.inputs["Metallic"].default_value = metallic

    mat.blend_method = "BLEND" if alpha < 1.0 else "OPAQUE"
    mat.show_transparent_back = True
    return mat


def init_materials():
    global MAT_STRUCTURAL, MAT_SECONDARY, MAT_PANEL, MAT_GLASS_PLACEHOLDER
    global MAT_DARK_PLACEHOLDER, MAT_METAL, MAT_RAIL, MAT_FLOOR, MAT_DETAIL
    global MAT_LIGHT_PLACEHOLDER

    MAT_STRUCTURAL = make_mat(
        "UNITY_ASSIGN_Concrete_Main",
        (0.72, 0.72, 0.70, 1.0),
        roughness=0.75,
    )
    MAT_SECONDARY = make_mat(
        "UNITY_ASSIGN_Concrete_Secondary",
        (0.56, 0.56, 0.55, 1.0),
        roughness=0.8,
    )
    MAT_PANEL = make_mat(
        "UNITY_ASSIGN_Exterior_Panel",
        (0.82, 0.82, 0.80, 1.0),
        roughness=0.45,
    )
    MAT_GLASS_PLACEHOLDER = make_mat(
        "UNITY_ASSIGN_Glass_CurtainWall",
        (0.55, 0.55, 0.55, 0.42),
        roughness=0.1,
        alpha=0.42,
    )
    MAT_DARK_PLACEHOLDER = make_mat(
        "UNITY_ASSIGN_Dark_Interior_Backplate",
        (0.08, 0.08, 0.08, 1.0),
        roughness=0.55,
    )
    MAT_METAL = make_mat(
        "UNITY_ASSIGN_Metal_Mullion",
        (0.65, 0.65, 0.64, 1.0),
        roughness=0.28,
        metallic=0.2,
    )
    MAT_RAIL = make_mat(
        "UNITY_ASSIGN_Railing",
        (0.78, 0.78, 0.76, 1.0),
        roughness=0.25,
        metallic=0.25,
    )
    MAT_FLOOR = make_mat(
        "UNITY_ASSIGN_Plaza_And_Deck",
        (0.68, 0.68, 0.66, 1.0),
        roughness=0.65,
    )
    MAT_DETAIL = make_mat(
        "UNITY_ASSIGN_Facade_Detail",
        (0.90, 0.90, 0.86, 1.0),
        roughness=0.38,
        metallic=0.1,
    )
    MAT_LIGHT_PLACEHOLDER = make_mat(
        "UNITY_ASSIGN_Warm_Light_Optional",
        (1.0, 0.84, 0.52, 1.0),
        roughness=0.2,
    )


def cube(name, loc, scale, mat, collection, bevel=0.0, segments=1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if mat:
        obj.data.materials.append(mat)
    link_to_collection(obj, collection)
    if bevel > 0.0:
        add_bevel(obj, bevel, segments)
    return obj


def cylinder(name, loc, radius, depth, mat, collection, vertices=32, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=loc,
    )
    obj = bpy.context.object
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    link_to_collection(obj, collection)
    if bevel > 0.0:
        add_bevel(obj, bevel, 1)
    return obj


def add_bevel(obj, amount, segments):
    bevel = obj.modifiers.new("Small_Bevel", "BEVEL")
    bevel.width = amount
    bevel.segments = segments
    bevel.affect = "EDGES"
    normal = obj.modifiers.new("Weighted_Normals", "WEIGHTED_NORMAL")
    normal.keep_sharp = True


def add_empty(name, collection, loc=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "CUBE"
    obj.empty_display_size = 1.0
    obj.location = loc
    collection.objects.link(obj)
    return obj


def parent(obj, parent_obj):
    obj.parent = parent_obj
    return obj


def make_group(name, collection, loc=(0, 0, 0)):
    return add_empty(name, collection, loc)


def add_repeated_posts(
    name,
    collection,
    parent_obj,
    start,
    end,
    count,
    post_scale,
    mat,
    bevel=0.0,
):
    if count <= 1:
        positions = [Vector(start)]
    else:
        positions = [
            Vector(start).lerp(Vector(end), i / (count - 1))
            for i in range(count)
        ]

    result = []
    for i, pos in enumerate(positions):
        post = cube(
            f"{name}_{i:02d}",
            pos,
            post_scale,
            mat,
            collection,
            bevel=bevel,
        )
        parent(post, parent_obj)
        result.append(post)
    return result


def add_railing_segment(
    name,
    collection,
    parent_obj,
    start,
    end,
    height=0.95,
    post_count=6,
    top_rail_thickness=0.06,
):
    start_v = Vector(start)
    end_v = Vector(end)
    direction = end_v - start_v
    length = direction.length
    if length <= 0.001:
        return []

    mid = (start_v + end_v) * 0.5
    angle = math.atan2(direction.y, direction.x)
    created = []

    rail = cube(
        f"{name}_TopRail",
        (mid.x, mid.y, mid.z + height),
        (length, top_rail_thickness, top_rail_thickness),
        MAT_RAIL,
        collection,
        bevel=0.015,
    )
    rail.rotation_euler[2] = angle
    parent(rail, parent_obj)
    created.append(rail)

    mid_rail = cube(
        f"{name}_MidRail",
        (mid.x, mid.y, mid.z + height * 0.55),
        (length, top_rail_thickness * 0.75, top_rail_thickness * 0.75),
        MAT_RAIL,
        collection,
        bevel=0.01,
    )
    mid_rail.rotation_euler[2] = angle
    parent(mid_rail, parent_obj)
    created.append(mid_rail)

    for i in range(post_count):
        t = i / max(1, post_count - 1)
        p = start_v.lerp(end_v, t)
        post = cube(
            f"{name}_Post_{i:02d}",
            (p.x, p.y, p.z + height * 0.5),
            (0.055, 0.055, height),
            MAT_RAIL,
            collection,
            bevel=0.01,
        )
        post.rotation_euler[2] = angle
        parent(post, parent_obj)
        created.append(post)

    return created


def add_stair_run(
    name,
    collection,
    parent_obj,
    start,
    width,
    step_count,
    step_depth,
    step_height,
    direction_y=-1.0,
):
    created = []
    sx, sy, sz = start

    for i in range(step_count):
        step = cube(
            f"{name}_Step_{i:02d}",
            (
                sx,
                sy + direction_y * step_depth * (i + 0.5),
                sz + step_height * (i + 0.5),
            ),
            (
                width,
                step_depth * 1.02,
                step_height,
            ),
            MAT_FLOOR,
            collection,
            bevel=0.01,
        )
        parent(step, parent_obj)
        created.append(step)

    top_z = sz + step_height * step_count
    end_y = sy + direction_y * step_depth * step_count

    stringer_left = cube(
        f"{name}_Stringer_L",
        (
            sx - width * 0.52,
            sy + direction_y * step_depth * step_count * 0.5,
            sz + step_height * step_count * 0.5,
        ),
        (0.12, step_depth * step_count, step_height * step_count),
        MAT_SECONDARY,
        collection,
        bevel=0.01,
    )
    stringer_right = cube(
        f"{name}_Stringer_R",
        (
            sx + width * 0.52,
            sy + direction_y * step_depth * step_count * 0.5,
            sz + step_height * step_count * 0.5,
        ),
        (0.12, step_depth * step_count, step_height * step_count),
        MAT_SECONDARY,
        collection,
        bevel=0.01,
    )
    parent(stringer_left, parent_obj)
    parent(stringer_right, parent_obj)
    created.extend([stringer_left, stringer_right])

    add_railing_segment(
        f"{name}_Rail_L",
        collection,
        parent_obj,
        (sx - width * 0.55, sy, sz + 0.1),
        (sx - width * 0.55, end_y, top_z),
        post_count=8,
    )
    add_railing_segment(
        f"{name}_Rail_R",
        collection,
        parent_obj,
        (sx + width * 0.55, sy, sz + 0.1),
        (sx + width * 0.55, end_y, top_z),
        post_count=8,
    )

    return created


def add_curtain_wall(
    name,
    collection,
    parent_obj,
    center,
    width,
    height,
    cols,
    rows,
    normal_axis="Y",
    normal_sign=-1,
    mullion_depth=0.12,
    glass_depth=0.055,
    start_pattern=0,
):
    cx, cy, cz = center
    panel_w = width / cols
    panel_h = height / rows
    created = []

    def pos_on_plane(local_a, local_z, depth_offset):
        if normal_axis == "Y":
            return (
                cx + local_a,
                cy + normal_sign * depth_offset,
                cz + local_z,
            )
        return (
            cx + normal_sign * depth_offset,
            cy + local_a,
            cz + local_z,
        )

    def scale_on_plane(size_a, size_z, depth):
        if normal_axis == "Y":
            return (size_a, depth, size_z)
        return (depth, size_a, size_z)

    for r in range(rows):
        for c in range(cols):
            local_a = -width * 0.5 + panel_w * (c + 0.5)
            local_z = -height * 0.5 + panel_h * (r + 0.5)
            panel_inset = 0.08 if (r + c + start_pattern) % 5 == 0 else 0.0
            panel = cube(
                f"{name}_GlassPanel_R{r:02d}_C{c:02d}",
                pos_on_plane(local_a, local_z, panel_inset),
                scale_on_plane(panel_w * 0.88, panel_h * 0.86, glass_depth),
                MAT_GLASS_PLACEHOLDER,
                collection,
                bevel=0.008,
            )
            parent(panel, parent_obj)
            created.append(panel)

    for c in range(cols + 1):
        local_a = -width * 0.5 + panel_w * c
        mullion = cube(
            f"{name}_Mullion_V_{c:02d}",
            pos_on_plane(local_a, 0.0, -0.02),
            scale_on_plane(0.055, height + 0.08, mullion_depth),
            MAT_METAL,
            collection,
            bevel=0.008,
        )
        parent(mullion, parent_obj)
        created.append(mullion)

    for r in range(rows + 1):
        local_z = -height * 0.5 + panel_h * r
        mullion = cube(
            f"{name}_Mullion_H_{r:02d}",
            pos_on_plane(0.0, local_z, -0.025),
            scale_on_plane(width + 0.08, 0.05, mullion_depth),
            MAT_METAL,
            collection,
            bevel=0.008,
        )
        parent(mullion, parent_obj)
        created.append(mullion)

    # Secondary hairline mullions for the richer high-rise facade look.
    for c in range(cols):
        if c % 2 != start_pattern % 2:
            continue
        local_a = -width * 0.5 + panel_w * (c + 0.5)
        hairline = cube(
            f"{name}_Fine_Mullion_V_{c:02d}",
            pos_on_plane(local_a, 0.0, -0.045),
            scale_on_plane(0.022, height * 0.92, mullion_depth * 0.55),
            MAT_DETAIL,
            collection,
            bevel=0.004,
        )
        parent(hairline, parent_obj)
        created.append(hairline)

    return created


def add_vertical_fin_field(
    name,
    collection,
    parent_obj,
    center,
    width,
    height,
    count,
    normal_axis="Y",
    normal_sign=-1,
    pattern_offset=0,
):
    cx, cy, cz = center
    spacing = width / count
    created = []

    def pos_on_plane(local_a, local_z):
        if normal_axis == "Y":
            return (
                cx + local_a,
                cy + normal_sign * 0.16,
                cz + local_z,
            )
        return (
            cx + normal_sign * 0.16,
            cy + local_a,
            cz + local_z,
        )

    def scale_on_plane(size_a, size_z, depth):
        if normal_axis == "Y":
            return (size_a, depth, size_z)
        return (depth, size_a, size_z)

    for i in range(count):
        group = (i + pattern_offset) % 6
        if group in (1, 5):
            continue

        local_a = -width * 0.5 + spacing * (i + 0.5)
        fin_height = height * (0.82 + 0.08 * (group % 3))
        fin_z = 0.04 * ((group % 2) - 0.5)
        fin_depth = 0.22 + 0.035 * (group % 3)
        fin = cube(
            f"{name}_Vertical_Fin_{i:02d}",
            pos_on_plane(local_a, fin_z),
            scale_on_plane(0.06, fin_height, fin_depth),
            MAT_DETAIL,
            collection,
            bevel=0.01,
        )
        parent(fin, parent_obj)
        created.append(fin)

    return created


def add_roof_frame(
    collection,
    parent_obj,
    center,
    width,
    depth,
    z,
):
    cx, cy = center
    created = []
    overhang = 0.55

    roof_slab = cube(
        "Main_Deep_Overhanging_Roof_Slab",
        (cx, cy, z),
        (width + overhang * 2, depth + overhang * 2, 0.22),
        MAT_SECONDARY,
        collection,
        bevel=0.035,
    )
    parent(roof_slab, parent_obj)
    created.append(roof_slab)

    soffit = cube(
        "Main_Roof_Dark_Recessed_Soffit",
        (cx, cy - 0.02, z - 0.16),
        (width + overhang * 1.5, depth + overhang * 1.5, 0.08),
        MAT_DARK_PLACEHOLDER,
        collection,
        bevel=0.02,
    )
    parent(soffit, parent_obj)
    created.append(soffit)

    beam_count_x = 7
    for i in range(beam_count_x):
        x = cx - width * 0.5 + width * i / (beam_count_x - 1)
        beam = cube(
            f"Roof_Soffit_Beam_X_{i:02d}",
            (x, cy, z - 0.34),
            (0.12, depth + overhang * 1.25, 0.18),
            MAT_METAL,
            collection,
            bevel=0.012,
        )
        parent(beam, parent_obj)
        created.append(beam)

    beam_count_y = 5
    for i in range(beam_count_y):
        y = cy - depth * 0.5 + depth * i / (beam_count_y - 1)
        beam = cube(
            f"Roof_Soffit_Beam_Y_{i:02d}",
            (cx, y, z - 0.36),
            (width + overhang * 1.2, 0.10, 0.14),
            MAT_METAL,
            collection,
            bevel=0.012,
        )
        parent(beam, parent_obj)
        created.append(beam)

    for i, x in enumerate([cx - width * 0.38, cx, cx + width * 0.38]):
        light = cube(
            f"Roof_Underside_Light_{i:02d}",
            (x, cy - depth * 0.42, z - 0.48),
            (0.18, 0.18, 0.05),
            MAT_LIGHT_PLACEHOLDER,
            collection,
            bevel=0.02,
        )
        parent(light, parent_obj)
        created.append(light)

    parapet_front = cube(
        "Roof_Thin_Front_Parapet",
        (cx, cy - depth * 0.5 - overhang * 0.5, z + 0.22),
        (width + overhang * 2, 0.12, 0.36),
        MAT_PANEL,
        collection,
        bevel=0.012,
    )
    parapet_back = cube(
        "Roof_Thin_Back_Parapet",
        (cx, cy + depth * 0.5 + overhang * 0.5, z + 0.22),
        (width + overhang * 2, 0.12, 0.36),
        MAT_PANEL,
        collection,
        bevel=0.012,
    )
    parapet_left = cube(
        "Roof_Thin_Left_Parapet",
        (cx - width * 0.5 - overhang * 0.5, cy, z + 0.22),
        (0.12, depth + overhang * 2, 0.36),
        MAT_PANEL,
        collection,
        bevel=0.012,
    )
    parapet_right = cube(
        "Roof_Thin_Right_Parapet",
        (cx + width * 0.5 + overhang * 0.5, cy, z + 0.22),
        (0.12, depth + overhang * 2, 0.36),
        MAT_PANEL,
        collection,
        bevel=0.012,
    )
    for obj in [parapet_front, parapet_back, parapet_left, parapet_right]:
        parent(obj, parent_obj)
        created.append(obj)

    return created


def add_ground_floor_colonnade(
    collection,
    parent_obj,
    x_center,
    y_front,
    z_base,
    width,
    count,
):
    created = []
    for i in range(count):
        x = x_center - width * 0.5 + width * i / max(1, count - 1)
        column = cube(
            f"Ground_Floor_Slender_Column_{i:02d}",
            (x, y_front, z_base + 0.92),
            (0.16, 0.16, 1.84),
            MAT_STRUCTURAL,
            collection,
            bevel=0.025,
            segments=1,
        )
        parent(column, parent_obj)
        created.append(column)

        base = cube(
            f"Ground_Floor_Column_Base_{i:02d}",
            (x, y_front, z_base + 0.06),
            (0.32, 0.32, 0.12),
            MAT_SECONDARY,
            collection,
            bevel=0.018,
        )
        parent(base, parent_obj)
        created.append(base)

    beam = cube(
        "Ground_Floor_Long_Canopy_Beam",
        (x_center, y_front, z_base + 1.88),
        (width + 0.55, 0.28, 0.20),
        MAT_SECONDARY,
        collection,
        bevel=0.018,
    )
    parent(beam, parent_obj)
    created.append(beam)

    return created


def add_lower_podium(
    collection,
    parent_obj,
    x_center,
    y_center,
    z_base,
):
    width = 12.4
    depth = 7.2
    height = 1.8
    created = []

    podium = cube(
        "Lower_Public_Podium_Main_Block",
        (x_center, y_center, z_base + height * 0.5),
        (width, depth, height),
        MAT_STRUCTURAL,
        collection,
        bevel=0.035,
        segments=1,
    )
    parent(podium, parent_obj)
    created.append(podium)

    undercroft = cube(
        "Lower_Public_Podium_Dark_Undercroft",
        (x_center + 1.4, y_center - depth * 0.48 - 0.03, z_base + 0.72),
        (width * 0.64, 0.16, 1.06),
        MAT_DARK_PLACEHOLDER,
        collection,
    )
    parent(undercroft, parent_obj)
    created.append(undercroft)

    add_curtain_wall(
        "Ground_Level_Recessed_Glass",
        collection,
        parent_obj,
        (x_center + 1.4, y_center - depth * 0.5 - 0.13, z_base + 0.95),
        width * 0.58,
        1.25,
        8,
        2,
        normal_axis="Y",
        normal_sign=-1,
        start_pattern=2,
    )

    left_solid = cube(
        "Lower_Podium_Left_Solid_Core",
        (x_center - width * 0.42, y_center - depth * 0.3, z_base + 0.82),
        (1.7, 1.55, 1.55),
        MAT_SECONDARY,
        collection,
        bevel=0.02,
    )
    parent(left_solid, parent_obj)
    created.append(left_solid)

    terrace = cube(
        "Second_Level_Public_Terrace",
        (x_center + 0.65, y_center - depth * 0.55, z_base + height + 0.05),
        (width * 0.72, 1.8, 0.12),
        MAT_FLOOR,
        collection,
        bevel=0.018,
    )
    parent(terrace, parent_obj)
    created.append(terrace)

    add_railing_segment(
        "Terrace_Front_Glass_Rail",
        collection,
        parent_obj,
        (x_center - width * 0.24, y_center - depth * 0.55 - 0.86, z_base + height + 0.06),
        (x_center + width * 0.43, y_center - depth * 0.55 - 0.86, z_base + height + 0.06),
        height=0.75,
        post_count=10,
        top_rail_thickness=0.045,
    )

    return created


def add_left_wing(
    collection,
    parent_obj,
    x_center,
    y_center,
    z_base,
):
    created = []
    wing_width = 6.3
    wing_depth = 4.6
    wing_height = 2.9

    body = cube(
        "Left_Research_Wing_Body",
        (x_center, y_center, z_base + wing_height * 0.5),
        (wing_width, wing_depth, wing_height),
        MAT_SECONDARY,
        collection,
        bevel=0.025,
    )
    parent(body, parent_obj)
    created.append(body)

    add_curtain_wall(
        "Left_Wing_Front_CurtainWall",
        collection,
        parent_obj,
        (x_center, y_center - wing_depth * 0.5 - 0.08, z_base + wing_height * 0.54),
        wing_width * 0.88,
        wing_height * 0.72,
        6,
        3,
        normal_axis="Y",
        normal_sign=-1,
        start_pattern=1,
    )

    roof = cube(
        "Left_Wing_Thin_Roof",
        (x_center, y_center, z_base + wing_height + 0.12),
        (wing_width + 0.55, wing_depth + 0.55, 0.18),
        MAT_PANEL,
        collection,
        bevel=0.025,
    )
    parent(roof, parent_obj)
    created.append(roof)

    connector = cube(
        "Left_Wing_Skybridge_Connector",
        (x_center + wing_width * 0.55, y_center - 0.2, z_base + 2.05),
        (2.8, 1.4, 0.62),
        MAT_GLASS_PLACEHOLDER,
        collection,
        bevel=0.018,
    )
    parent(connector, parent_obj)
    created.append(connector)

    for i in range(5):
        x = x_center - wing_width * 0.4 + i * wing_width * 0.2
        fin = cube(
            f"Left_Wing_Roof_Fin_{i:02d}",
            (x, y_center - wing_depth * 0.52, z_base + wing_height + 0.52),
            (0.08, 0.28, 0.62),
            MAT_DETAIL,
            collection,
            bevel=0.006,
        )
        parent(fin, parent_obj)
        created.append(fin)

    return created


def add_back_cylindrical_volume(
    collection,
    parent_obj,
    x,
    y,
    z_base,
):
    created = []
    radius = 2.0
    height = 4.7

    core = cylinder(
        "Rear_Rounded_Lab_Volume_Core",
        (x, y, z_base + height * 0.5),
        radius,
        height,
        MAT_SECONDARY,
        collection,
        vertices=48,
        bevel=0.01,
    )
    parent(core, parent_obj)
    created.append(core)

    glass_band_count = 5
    for i in range(glass_band_count):
        band = cylinder(
            f"Rear_Rounded_Glass_Band_{i:02d}",
            (x, y, z_base + 0.72 + i * 0.72),
            radius + 0.025,
            0.25,
            MAT_GLASS_PLACEHOLDER,
            collection,
            vertices=48,
        )
        band.rotation_euler[0] = math.radians(90)
        parent(band, parent_obj)
        created.append(band)

    for i in range(16):
        angle = math.tau * i / 16
        px = x + math.cos(angle) * (radius + 0.06)
        py = y + math.sin(angle) * (radius + 0.06)
        post = cube(
            f"Rear_Rounded_Vertical_Rib_{i:02d}",
            (px, py, z_base + height * 0.5),
            (0.055, 0.055, height * 0.93),
            MAT_METAL,
            collection,
            bevel=0.006,
        )
        post.rotation_euler[2] = angle
        parent(post, parent_obj)
        created.append(post)

    roof = cylinder(
        "Rear_Rounded_Roof_Cap",
        (x, y, z_base + height + 0.12),
        radius + 0.12,
        0.22,
        MAT_PANEL,
        collection,
        vertices=48,
        bevel=0.015,
    )
    parent(roof, parent_obj)
    created.append(roof)

    return created


def add_entry_plaza(
    collection,
    parent_obj,
    x_center,
    y_front,
    z_base,
):
    created = []

    plaza = cube(
        "Front_Entry_Plaza_Slab",
        (x_center, y_front - 4.4, z_base - 0.035),
        (15.0, 7.2, 0.07),
        MAT_FLOOR,
        collection,
        bevel=0.02,
    )
    parent(plaza, parent_obj)
    created.append(plaza)

    main_stair = add_stair_run(
        "Grand_Central_Exterior_Stair",
        collection,
        parent_obj,
        (x_center + 1.5, y_front - 2.3, z_base + 0.02),
        width=3.2,
        step_count=13,
        step_depth=0.34,
        step_height=0.115,
        direction_y=1.0,
    )
    created.extend(main_stair)

    landing = cube(
        "Grand_Stair_Upper_Landing",
        (x_center + 1.5, y_front + 2.24, z_base + 1.55),
        (3.7, 1.25, 0.16),
        MAT_FLOOR,
        collection,
        bevel=0.018,
    )
    parent(landing, parent_obj)
    created.append(landing)

    lower_side_stair = add_stair_run(
        "Side_Low_Exterior_Stair",
        collection,
        parent_obj,
        (x_center - 4.4, y_front - 2.0, z_base + 0.02),
        width=1.4,
        step_count=7,
        step_depth=0.32,
        step_height=0.10,
        direction_y=1.0,
    )
    created.extend(lower_side_stair)

    for i, x in enumerate([x_center - 5.2, x_center - 2.8, x_center + 4.8, x_center + 6.2]):
        planter = cube(
            f"Plaza_Rectangular_Planter_{i:02d}",
            (x, y_front - 5.8 + (i % 2) * 1.0, z_base + 0.18),
            (1.55, 0.72, 0.36),
            MAT_SECONDARY,
            collection,
            bevel=0.035,
            segments=1,
        )
        parent(planter, parent_obj)
        created.append(planter)

        soil = cube(
            f"Plaza_Planter_Insert_{i:02d}",
            (x, y_front - 5.8 + (i % 2) * 1.0, z_base + 0.39),
            (1.38, 0.55, 0.04),
            MAT_DARK_PLACEHOLDER,
            collection,
            bevel=0.015,
        )
        parent(soil, parent_obj)
        created.append(soil)

    return created


def add_facade_balconies(
    collection,
    parent_obj,
    x_center,
    y_front,
    z_base,
):
    created = []
    balcony_specs = [
        (-2.9, 2.05, 1.9),
        (1.3, 3.20, 2.4),
        (3.4, 4.35, 1.6),
    ]

    for i, (x_offset, z, width) in enumerate(balcony_specs):
        deck = cube(
            f"Glass_Box_Facade_Balcony_Deck_{i:02d}",
            (x_center + x_offset, y_front - 0.35, z_base + z),
            (width, 0.78, 0.10),
            MAT_FLOOR,
            collection,
            bevel=0.012,
        )
        parent(deck, parent_obj)
        created.append(deck)

        add_railing_segment(
            f"Glass_Box_Facade_Balcony_Rail_{i:02d}",
            collection,
            parent_obj,
            (x_center + x_offset - width * 0.5, y_front - 0.78, z_base + z + 0.05),
            (x_center + x_offset + width * 0.5, y_front - 0.78, z_base + z + 0.05),
            height=0.58,
            post_count=4,
            top_rail_thickness=0.035,
        )

        side_l = cube(
            f"Glass_Box_Balcony_Side_L_{i:02d}",
            (x_center + x_offset - width * 0.5, y_front - 0.43, z_base + z + 0.34),
            (0.045, 0.64, 0.55),
            MAT_GLASS_PLACEHOLDER,
            collection,
            bevel=0.006,
        )
        side_r = cube(
            f"Glass_Box_Balcony_Side_R_{i:02d}",
            (x_center + x_offset + width * 0.5, y_front - 0.43, z_base + z + 0.34),
            (0.045, 0.64, 0.55),
            MAT_GLASS_PLACEHOLDER,
            collection,
            bevel=0.006,
        )
        parent(side_l, parent_obj)
        parent(side_r, parent_obj)
        created.extend([side_l, side_r])

    return created


def add_roof_equipment(
    collection,
    parent_obj,
    x_center,
    y_center,
    z,
):
    created = []

    for i in range(5):
        unit = cube(
            f"Roof_Mechanical_Box_{i:02d}",
            (
                x_center - 4.1 + i * 1.2,
                y_center + 1.6 + (i % 2) * 0.46,
                z + 0.22,
            ),
            (
                0.72,
                0.95,
                0.42,
            ),
            MAT_METAL,
            collection,
            bevel=0.025,
        )
        parent(unit, parent_obj)
        created.append(unit)

        grille_count = 4
        for g in range(grille_count):
            grille = cube(
                f"Roof_Mechanical_Box_{i:02d}_Grille_{g:02d}",
                (
                    x_center - 4.1 + i * 1.2 - 0.28 + g * 0.18,
                    y_center + 1.6 + (i % 2) * 0.46 - 0.49,
                    z + 0.28,
                ),
                (0.045, 0.035, 0.28),
                MAT_DARK_PLACEHOLDER,
                collection,
            )
            parent(grille, parent_obj)
            created.append(grille)

    duct = cube(
        "Roof_Long_Service_Duct",
        (x_center + 1.8, y_center + 1.95, z + 0.31),
        (3.3, 0.38, 0.28),
        MAT_METAL,
        collection,
        bevel=0.018,
    )
    parent(duct, parent_obj)
    created.append(duct)

    antenna = cylinder(
        "Roof_Slim_Antenna_Mast",
        (x_center + 4.7, y_center + 1.2, z + 0.82),
        0.035,
        1.35,
        MAT_METAL,
        collection,
        vertices=12,
        bevel=0.004,
    )
    parent(antenna, parent_obj)
    created.append(antenna)

    return created


def add_main_tower(collection, parent_obj):
    created = []
    x_center = 0.0
    y_center = 0.0
    z_base = 1.8
    width = 9.2
    depth = 6.4
    floors = 4
    floor_h = 1.12
    tower_h = floors * floor_h

    interior = cube(
        "Main_Tower_Dark_Recessed_Interior_Mass",
        (x_center, y_center + 0.28, z_base + tower_h * 0.5),
        (width * 0.88, depth * 0.72, tower_h * 0.96),
        MAT_DARK_PLACEHOLDER,
        collection,
        bevel=0.015,
    )
    parent(interior, parent_obj)
    created.append(interior)

    floorplates = []
    for i in range(floors + 1):
        z = z_base + i * floor_h
        plate = cube(
            f"Main_Tower_Internal_FloorPlate_{i:02d}",
            (x_center, y_center + 0.05, z),
            (width * 0.92, depth * 0.78, 0.075),
            MAT_FLOOR,
            collection,
            bevel=0.006,
        )
        parent(plate, parent_obj)
        floorplates.append(plate)
        created.append(plate)

    front_y = y_center - depth * 0.5 - 0.08
    add_curtain_wall(
        "Main_Tower_Front_CurtainWall",
        collection,
        parent_obj,
        (x_center, front_y, z_base + tower_h * 0.5),
        width,
        tower_h,
        11,
        4,
        normal_axis="Y",
        normal_sign=-1,
        start_pattern=0,
    )
    add_vertical_fin_field(
        "Main_Tower_Front_Deep_Fins",
        collection,
        parent_obj,
        (x_center, front_y - 0.03, z_base + tower_h * 0.5),
        width,
        tower_h,
        18,
        normal_axis="Y",
        normal_sign=-1,
        pattern_offset=2,
    )

    right_x = x_center + width * 0.5 + 0.08
    add_curtain_wall(
        "Main_Tower_Right_CurtainWall",
        collection,
        parent_obj,
        (right_x, y_center, z_base + tower_h * 0.5),
        depth,
        tower_h,
        7,
        4,
        normal_axis="X",
        normal_sign=1,
        start_pattern=1,
    )
    add_vertical_fin_field(
        "Main_Tower_Right_Deep_Fins",
        collection,
        parent_obj,
        (right_x + 0.02, y_center, z_base + tower_h * 0.5),
        depth,
        tower_h,
        11,
        normal_axis="X",
        normal_sign=1,
        pattern_offset=0,
    )

    left_wall = cube(
        "Main_Tower_Left_Solid_Service_Wall",
        (x_center - width * 0.5 - 0.04, y_center + 0.1, z_base + tower_h * 0.5),
        (0.18, depth * 0.82, tower_h),
        MAT_PANEL,
        collection,
        bevel=0.012,
    )
    parent(left_wall, parent_obj)
    created.append(left_wall)

    rear_wall = cube(
        "Main_Tower_Rear_Solid_Service_Wall",
        (x_center, y_center + depth * 0.5 + 0.04, z_base + tower_h * 0.5),
        (width, 0.18, tower_h),
        MAT_PANEL,
        collection,
        bevel=0.012,
    )
    parent(rear_wall, parent_obj)
    created.append(rear_wall)

    add_facade_balconies(collection, parent_obj, x_center, front_y, 0.0)

    roof_z = z_base + tower_h + 0.18
    add_roof_frame(collection, parent_obj, (x_center, y_center), width, depth, roof_z)
    add_roof_equipment(collection, parent_obj, x_center, y_center, roof_z + 0.16)

    corner_column_positions = [
        (x_center - width * 0.5 + 0.22, y_center - depth * 0.5 + 0.22),
        (x_center + width * 0.5 - 0.22, y_center - depth * 0.5 + 0.22),
        (x_center - width * 0.5 + 0.22, y_center + depth * 0.5 - 0.22),
        (x_center + width * 0.5 - 0.22, y_center + depth * 0.5 - 0.22),
    ]
    for i, (x, y) in enumerate(corner_column_positions):
        col = cube(
            f"Main_Tower_Corner_Structure_{i:02d}",
            (x, y, z_base + tower_h * 0.5),
            (0.18, 0.18, tower_h + 0.05),
            MAT_METAL,
            collection,
            bevel=0.012,
        )
        parent(col, parent_obj)
        created.append(col)

    return created


def add_site_details(collection, parent_obj):
    created = []

    # Reference scale strips for surrounding walkways and hardscape.
    front_walk = cube(
        "Site_Front_Wide_Pedestrian_Walk",
        (0.0, -10.1, -0.055),
        (18.5, 2.4, 0.06),
        MAT_FLOOR,
        collection,
        bevel=0.012,
    )
    parent(front_walk, parent_obj)
    created.append(front_walk)

    road_edge = cube(
        "Site_Road_Edge_Reference_Curb",
        (0.0, -11.45, 0.02),
        (18.5, 0.18, 0.14),
        MAT_SECONDARY,
        collection,
        bevel=0.012,
    )
    parent(road_edge, parent_obj)
    created.append(road_edge)

    for i in range(11):
        paver = cube(
            f"Site_Plaza_Joint_Line_X_{i:02d}",
            (-8.0 + i * 1.6, -8.9, 0.005),
            (0.025, 3.3, 0.012),
            MAT_SECONDARY,
            collection,
        )
        parent(paver, parent_obj)
        created.append(paver)

    for i in range(5):
        paver = cube(
            f"Site_Plaza_Joint_Line_Y_{i:02d}",
            (0.0, -10.35 + i * 0.72, 0.006),
            (18.0, 0.02, 0.012),
            MAT_SECONDARY,
            collection,
        )
        parent(paver, parent_obj)
        created.append(paver)

    bollard_positions = [
        (-6.8, -8.0),
        (-5.8, -8.0),
        (5.8, -8.0),
        (6.8, -8.0),
        (7.8, -8.0),
    ]
    for i, (x, y) in enumerate(bollard_positions):
        bollard = cylinder(
            f"Site_Short_Bollard_{i:02d}",
            (x, y, 0.28),
            0.07,
            0.56,
            MAT_METAL,
            collection,
            vertices=16,
            bevel=0.006,
        )
        parent(bollard, parent_obj)
        created.append(bollard)

    return created


def create_premium_lab():
    collection = make_collection(COLLECTION_NAME)
    root = make_group("Premium_Semicon_Lab_Root", collection)

    add_lower_podium(collection, root, 0.0, -0.35, 0.0)
    add_main_tower(collection, root)
    add_left_wing(collection, root, -8.2, 1.25, 0.0)
    add_back_cylindrical_volume(collection, root, 6.9, 2.75, 0.0)
    add_entry_plaza(collection, root, 0.0, -3.95, 0.0)
    add_ground_floor_colonnade(collection, root, 0.0, -4.02, 0.0, 8.2, 7)
    add_site_details(collection, root)

    # Extra facade articulation: staggered external vertical glass boxes seen in
    # the reference photo.
    stagger_specs = [
        (-1.95, -3.56, 3.15, 0.62, 0.52, 1.25),
        (-1.25, -3.62, 3.45, 0.58, 0.62, 1.55),
        (-0.54, -3.58, 3.70, 0.62, 0.52, 1.38),
        (0.16, -3.64, 3.95, 0.58, 0.62, 1.72),
        (0.86, -3.58, 4.18, 0.62, 0.52, 1.42),
    ]
    for i, (x, y, z, w, d, h) in enumerate(stagger_specs):
        box = cube(
            f"Main_Facade_Staggered_Glass_Volume_{i:02d}",
            (x, y, z),
            (w, d, h),
            MAT_GLASS_PLACEHOLDER,
            collection,
            bevel=0.012,
        )
        parent(box, root)

        cap = cube(
            f"Main_Facade_Staggered_Box_Cap_{i:02d}",
            (x, y - d * 0.53, z + h * 0.5 + 0.03),
            (w + 0.08, 0.045, 0.08),
            MAT_METAL,
            collection,
            bevel=0.005,
        )
        parent(cap, root)

    return root, collection


def add_camera_and_lighting(collection):
    bpy.ops.object.light_add(type="SUN", location=(-6, -10, 12))
    sun = bpy.context.object
    sun.name = "Preview_Sun"
    sun.rotation_euler = (math.radians(48), 0, math.radians(-35))
    sun.data.energy = 2.5
    link_to_collection(sun, collection)

    bpy.ops.object.light_add(type="AREA", location=(0, -7, 7))
    area = bpy.context.object
    area.name = "Preview_Front_Area_Light"
    area.data.energy = 420
    area.data.size = 6.0
    link_to_collection(area, collection)

    bpy.ops.object.camera_add(
        location=(11.5, -16.0, 7.3),
        rotation=(math.radians(62), 0, math.radians(37)),
    )
    cam = bpy.context.object
    cam.name = "Preview_Camera"
    cam.data.lens = 30
    bpy.context.scene.camera = cam
    link_to_collection(cam, collection)


def set_origin_and_units():
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def export_selected_fbx(root, path):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
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
    set_origin_and_units()
    init_materials()
    root, collection = create_premium_lab()
    add_camera_and_lighting(collection)

    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 96
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"

    if EXPORT_FBX:
        export_selected_fbx(root, FBX_PATH)


main()
