import bpy
import math
from mathutils import Vector


# Reference-focused semiconductor lab building generator.
#
# 목표:
# - 사진처럼 큰 흰색 직육면체 매스 중심
# - 상부 캔틸레버 박스
# - 전면 대형 검은 유리면
# - 긴 하부 연구동
# - 매우 많은 작은 창문 패턴
# - 불필요한 주변 건물/공사장/도로 없음
#
# 실행:
# Blender > Scripting > Text > Open > Run Script
#
# Unity 작업을 쉽게 하기 위해 material 이름을 의미별로 분리했습니다.
# 최종 유리/벽 재질은 Unity에서 교체하세요.


EXPORT_FBX = False
FBX_PATH = "/Users/jinyeong/Desktop/SemiconCity/Assets/Models/Block_Window_Semicon_Lab.fbx"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_collection(name):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def link_to(collection, obj):
    for user_collection in obj.users_collection:
        user_collection.objects.unlink(obj)
    collection.objects.link(obj)


def make_mat(name, color, roughness=0.5, metallic=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True

    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha

    mat.blend_method = "BLEND" if alpha < 1.0 else "OPAQUE"
    mat.show_transparent_back = True
    return mat


MAT_WHITE = None
MAT_WHITE_SIDE = None
MAT_DARK_GLASS = None
MAT_WINDOW = None
MAT_WARM_WINDOW = None
MAT_FRAME = None
MAT_SHADOW = None
MAT_ROOF = None
MAT_UNDERSIDE = None
MAT_COLUMN = None


def init_materials():
    global MAT_WHITE
    global MAT_WHITE_SIDE
    global MAT_DARK_GLASS
    global MAT_WINDOW
    global MAT_WARM_WINDOW
    global MAT_FRAME
    global MAT_SHADOW
    global MAT_ROOF
    global MAT_UNDERSIDE
    global MAT_COLUMN

    MAT_WHITE = make_mat(
        "UNITY_ASSIGN_White_Exterior_Panel",
        (0.90, 0.91, 0.89, 1.0),
        roughness=0.62,
    )
    MAT_WHITE_SIDE = make_mat(
        "UNITY_ASSIGN_White_Side_Variation",
        (0.82, 0.84, 0.83, 1.0),
        roughness=0.68,
    )
    MAT_DARK_GLASS = make_mat(
        "UNITY_ASSIGN_Dark_Large_Glass",
        (0.04, 0.06, 0.07, 1.0),
        roughness=0.22,
    )
    MAT_WINDOW = make_mat(
        "UNITY_ASSIGN_Recessed_Window",
        (0.30, 0.38, 0.42, 1.0),
        roughness=0.28,
    )
    MAT_WARM_WINDOW = make_mat(
        "UNITY_ASSIGN_Warm_Lit_Window",
        (0.95, 0.72, 0.30, 1.0),
        roughness=0.2,
    )
    MAT_FRAME = make_mat(
        "UNITY_ASSIGN_Window_Frame",
        (0.96, 0.97, 0.95, 1.0),
        roughness=0.5,
        metallic=0.05,
    )
    MAT_SHADOW = make_mat(
        "UNITY_ASSIGN_Deep_Shadow_Cavity",
        (0.025, 0.025, 0.025, 1.0),
        roughness=0.6,
    )
    MAT_ROOF = make_mat(
        "UNITY_ASSIGN_Roof_Parapet",
        (0.74, 0.76, 0.75, 1.0),
        roughness=0.65,
    )
    MAT_UNDERSIDE = make_mat(
        "UNITY_ASSIGN_Cantilever_Underside",
        (0.50, 0.54, 0.55, 1.0),
        roughness=0.58,
    )
    MAT_COLUMN = make_mat(
        "UNITY_ASSIGN_Structure_Column",
        (0.78, 0.79, 0.77, 1.0),
        roughness=0.55,
    )


def cube(name, loc, scale, mat, collection, bevel=0.0, segments=1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    if mat is not None:
        obj.data.materials.append(mat)

    link_to(collection, obj)

    if bevel > 0.0:
        bevel_mod = obj.modifiers.new("Small_Bevel", "BEVEL")
        bevel_mod.width = bevel
        bevel_mod.segments = segments
        bevel_mod.affect = "EDGES"
        normal_mod = obj.modifiers.new("Weighted_Normals", "WEIGHTED_NORMAL")
        normal_mod.keep_sharp = True

    return obj


def parent(child, parent_obj):
    child.parent = parent_obj
    return child


def empty(name, collection, loc=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "CUBE"
    obj.empty_display_size = 1.0
    obj.location = loc
    collection.objects.link(obj)
    return obj


def add_window_unit(
    name,
    collection,
    parent_obj,
    center,
    width,
    height,
    axis="Y",
    direction=-1,
    lit=False,
    frame=True,
):
    x, y, z = center
    depth = 0.035
    mat = MAT_WARM_WINDOW if lit else MAT_WINDOW

    if axis == "Y":
        window_scale = (width, depth, height)
        frame_top_scale = (width + 0.045, depth * 1.35, 0.025)
        frame_side_scale = (0.025, depth * 1.35, height + 0.04)
        pos = (x, y + direction * 0.018, z)
        top_pos = (x, y + direction * 0.024, z + height * 0.5 + 0.02)
        bot_pos = (x, y + direction * 0.024, z - height * 0.5 - 0.02)
        left_pos = (x - width * 0.5 - 0.02, y + direction * 0.024, z)
        right_pos = (x + width * 0.5 + 0.02, y + direction * 0.024, z)
    else:
        window_scale = (depth, width, height)
        frame_top_scale = (depth * 1.35, width + 0.045, 0.025)
        frame_side_scale = (depth * 1.35, 0.025, height + 0.04)
        pos = (x + direction * 0.018, y, z)
        top_pos = (x + direction * 0.024, y, z + height * 0.5 + 0.02)
        bot_pos = (x + direction * 0.024, y, z - height * 0.5 - 0.02)
        left_pos = (x + direction * 0.024, y - width * 0.5 - 0.02, z)
        right_pos = (x + direction * 0.024, y + width * 0.5 + 0.02, z)

    win = cube(
        f"{name}_Glass",
        pos,
        window_scale,
        mat,
        collection,
        bevel=0.003,
    )
    parent(win, parent_obj)

    if not frame:
        return [win]

    top = cube(f"{name}_Frame_T", top_pos, frame_top_scale, MAT_FRAME, collection)
    bottom = cube(f"{name}_Frame_B", bot_pos, frame_top_scale, MAT_FRAME, collection)
    left = cube(f"{name}_Frame_L", left_pos, frame_side_scale, MAT_FRAME, collection)
    right = cube(f"{name}_Frame_R", right_pos, frame_side_scale, MAT_FRAME, collection)

    for obj in [top, bottom, left, right]:
        parent(obj, parent_obj)

    return [win, top, bottom, left, right]


def add_window_strip(
    name,
    collection,
    parent_obj,
    start_x,
    end_x,
    y,
    z,
    count,
    width,
    height,
    pattern_offset=0,
):
    for i in range(count):
        t = i / max(1, count - 1)
        x = start_x + (end_x - start_x) * t
        lit = (i + pattern_offset) % 9 in (2, 7)
        add_window_unit(
            f"{name}_{i:02d}",
            collection,
            parent_obj,
            (x, y, z),
            width,
            height,
            axis="Y",
            direction=-1,
            lit=lit,
            frame=True,
        )


def add_side_window_strip(
    name,
    collection,
    parent_obj,
    x,
    start_y,
    end_y,
    z,
    count,
    width,
    height,
    pattern_offset=0,
):
    for i in range(count):
        t = i / max(1, count - 1)
        y = start_y + (end_y - start_y) * t
        lit = (i + pattern_offset) % 8 == 3
        add_window_unit(
            f"{name}_{i:02d}",
            collection,
            parent_obj,
            (x, y, z),
            width,
            height,
            axis="X",
            direction=1,
            lit=lit,
            frame=True,
        )


def add_randomized_small_windows(
    name,
    collection,
    parent_obj,
    start_x,
    end_x,
    y,
    z_base,
    rows,
    cols,
    row_spacing,
    col_spacing,
    width,
    height,
    pattern_seed,
):
    for row in range(rows):
        for col in range(cols):
            skip_pattern = (row * 7 + col * 5 + pattern_seed) % 11
            if skip_pattern in (0, 6):
                continue

            x = start_x + col * col_spacing
            if x > end_x:
                continue

            z = z_base + row * row_spacing
            local_w = width * (0.7 if skip_pattern in (3, 8) else 1.0)
            local_h = height * (1.3 if skip_pattern == 5 else 1.0)
            lit = skip_pattern in (2, 9)

            add_window_unit(
                f"{name}_R{row:02d}_C{col:02d}",
                collection,
                parent_obj,
                (x, y, z),
                local_w,
                local_h,
                axis="Y",
                direction=-1,
                lit=lit,
                frame=True,
            )


def add_front_large_glass_box(
    collection,
    parent_obj,
    x,
    y,
    z,
    width,
    height,
    cols,
    rows,
):
    back = cube(
        "Front_Large_Glass_Back_Shadow",
        (x, y + 0.045, z),
        (width + 0.18, 0.07, height + 0.18),
        MAT_SHADOW,
        collection,
    )
    parent(back, parent_obj)

    panel_w = width / cols
    panel_h = height / rows

    for r in range(rows):
        for c in range(cols):
            px = x - width * 0.5 + panel_w * (c + 0.5)
            pz = z - height * 0.5 + panel_h * (r + 0.5)
            lit = (r * 3 + c) % 13 in (4, 9)
            add_window_unit(
                f"Front_Large_Glass_R{r:02d}_C{c:02d}",
                collection,
                parent_obj,
                (px, y - 0.02, pz),
                panel_w * 0.92,
                panel_h * 0.88,
                axis="Y",
                direction=-1,
                lit=lit,
                frame=False,
            )

    for c in range(cols + 1):
        px = x - width * 0.5 + panel_w * c
        mullion = cube(
            f"Front_Large_Glass_Mullion_V_{c:02d}",
            (px, y - 0.045, z),
            (0.045, 0.085, height + 0.12),
            MAT_FRAME,
            collection,
        )
        parent(mullion, parent_obj)

    for r in range(rows + 1):
        pz = z - height * 0.5 + panel_h * r
        mullion = cube(
            f"Front_Large_Glass_Mullion_H_{r:02d}",
            (x, y - 0.05, pz),
            (width + 0.12, 0.085, 0.04),
            MAT_FRAME,
            collection,
        )
        parent(mullion, parent_obj)

    frame_top = cube(
        "Front_Large_Glass_Thick_Frame_Top",
        (x, y - 0.09, z + height * 0.5 + 0.18),
        (width + 0.5, 0.20, 0.26),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    frame_bottom = cube(
        "Front_Large_Glass_Thick_Frame_Bottom",
        (x, y - 0.09, z - height * 0.5 - 0.18),
        (width + 0.5, 0.20, 0.26),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    frame_left = cube(
        "Front_Large_Glass_Thick_Frame_Left",
        (x - width * 0.5 - 0.18, y - 0.09, z),
        (0.26, 0.20, height + 0.5),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    frame_right = cube(
        "Front_Large_Glass_Thick_Frame_Right",
        (x + width * 0.5 + 0.18, y - 0.09, z),
        (0.26, 0.20, height + 0.5),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )

    for obj in [frame_top, frame_bottom, frame_left, frame_right]:
        parent(obj, parent_obj)


def add_black_upper_glass(
    collection,
    parent_obj,
    x,
    y,
    z,
    width,
    height,
):
    cavity = cube(
        "Upper_Block_Black_Glass_Recess",
        (x, y + 0.05, z),
        (width + 0.18, 0.10, height + 0.18),
        MAT_SHADOW,
        collection,
    )
    parent(cavity, parent_obj)

    glass = cube(
        "Upper_Block_Main_Black_Glass_Surface",
        (x, y - 0.025, z),
        (width, 0.055, height),
        MAT_DARK_GLASS,
        collection,
        bevel=0.006,
    )
    parent(glass, parent_obj)

    cols = 22
    panel_w = width / cols
    for i in range(cols + 1):
        px = x - width * 0.5 + panel_w * i
        mullion = cube(
            f"Upper_Block_Black_Glass_Fine_Mullion_{i:02d}",
            (px, y - 0.055, z),
            (0.018, 0.075, height * 0.96),
            MAT_FRAME,
            collection,
        )
        parent(mullion, parent_obj)

    # Thin interior light line like the reference image.
    for i in range(18):
        px = x - width * 0.5 + 0.35 + i * (width - 0.7) / 17
        light = cube(
            f"Upper_Block_Interior_Light_Dash_{i:02d}",
            (px, y - 0.065, z + height * 0.32),
            (0.22, 0.04, 0.035),
            MAT_WARM_WINDOW,
            collection,
        )
        parent(light, parent_obj)


def add_under_cantilever_columns(
    collection,
    parent_obj,
    x_start,
    x_end,
    y,
    z_base,
    height,
    count,
):
    for i in range(count):
        x = x_start + (x_end - x_start) * i / max(1, count - 1)
        col = cube(
            f"Cantilever_Slender_Support_Column_{i:02d}",
            (x, y, z_base + height * 0.5),
            (0.13, 0.13, height),
            MAT_COLUMN,
            collection,
            bevel=0.015,
        )
        parent(col, parent_obj)


def add_recessed_pilotis(
    collection,
    parent_obj,
    x,
    y,
    z,
    width,
    height,
):
    recess = cube(
        "Central_Pilotis_Deep_Recess_Shadow",
        (x, y, z),
        (width, 0.16, height),
        MAT_SHADOW,
        collection,
    )
    parent(recess, parent_obj)

    cols = 9
    for i in range(cols):
        px = x - width * 0.5 + width * (i + 0.5) / cols
        strip = cube(
            f"Central_Pilotis_Vertical_Window_{i:02d}",
            (px, y - 0.09, z),
            (width / cols * 0.46, 0.05, height * 0.88),
            MAT_WINDOW,
            collection,
        )
        parent(strip, parent_obj)

    for i in range(8):
        px = x - width * 0.5 + width * i / 7
        col = cube(
            f"Central_Pilotis_Thin_Column_{i:02d}",
            (px, y - 0.16, z),
            (0.075, 0.10, height * 1.05),
            MAT_COLUMN,
            collection,
            bevel=0.006,
        )
        parent(col, parent_obj)


def add_top_side_windows(
    collection,
    parent_obj,
    side_x,
    start_y,
    end_y,
    z_base,
):
    rows = 4
    count = 19
    for row in range(rows):
        z = z_base + row * 0.55
        add_side_window_strip(
            f"Upper_Right_Long_Side_Window_Row_{row:02d}",
            collection,
            parent_obj,
            side_x,
            start_y,
            end_y,
            z,
            count,
            0.18,
            0.25,
            pattern_offset=row * 3,
        )


def add_long_lower_building_windows(collection, parent_obj, y_front, z_base):
    rows = 4
    cols = 44
    for row in range(rows):
        z = z_base + row * 0.45
        for col in range(cols):
            if (row * 13 + col * 3) % 17 in (0, 6, 11):
                continue

            x = -11.5 + col * 0.55
            width = 0.22 if (col + row) % 5 else 0.34
            height = 0.22 if (col + row) % 4 else 0.34
            lit = (col + row * 5) % 19 in (4, 12)

            add_window_unit(
                f"Lower_Long_Bar_Window_R{row:02d}_C{col:02d}",
                collection,
                parent_obj,
                (x, y_front, z),
                width,
                height,
                axis="Y",
                direction=-1,
                lit=lit,
                frame=True,
            )


def add_parapet_and_edges(collection, parent_obj, name, center, size):
    x, y, z = center
    sx, sy, sz = size
    t = 0.13

    parts = [
        (
            f"{name}_Front_Edge",
            (x, y - sy * 0.5 - t * 0.5, z),
            (sx + t * 2, t, sz),
        ),
        (
            f"{name}_Back_Edge",
            (x, y + sy * 0.5 + t * 0.5, z),
            (sx + t * 2, t, sz),
        ),
        (
            f"{name}_Left_Edge",
            (x - sx * 0.5 - t * 0.5, y, z),
            (t, sy + t * 2, sz),
        ),
        (
            f"{name}_Right_Edge",
            (x + sx * 0.5 + t * 0.5, y, z),
            (t, sy + t * 2, sz),
        ),
    ]

    for part_name, loc, scale in parts:
        obj = cube(part_name, loc, scale, MAT_FRAME, collection, bevel=0.006)
        parent(obj, parent_obj)


def create_reference_building():
    collection = make_collection("Block_Window_Semicon_Lab")
    root = empty("Block_Window_Semicon_Lab_Root", collection)

    # Long lower white bar.
    lower = cube(
        "Long_Lower_Research_Bar_White_Mass",
        (0.0, 0.0, 1.35),
        (24.5, 4.2, 2.7),
        MAT_WHITE,
        collection,
        bevel=0.025,
    )
    parent(lower, root)

    lower_underside = cube(
        "Long_Lower_Bar_Dark_Recessed_Underside",
        (-6.0, -1.92, 0.52),
        (8.8, 0.18, 0.82),
        MAT_SHADOW,
        collection,
    )
    parent(lower_underside, root)

    add_long_lower_building_windows(collection, root, y_front=-2.13, z_base=0.62)

    # Central front black glass box on the lower mass.
    add_front_large_glass_box(
        collection,
        root,
        x=5.2,
        y=-2.25,
        z=1.36,
        width=5.3,
        height=2.1,
        cols=9,
        rows=4,
    )

    # Upper cantilevered white block.
    upper = cube(
        "Upper_Cantilevered_Box_White_Mass",
        (-2.3, -0.15, 5.05),
        (10.5, 5.0, 3.15),
        MAT_WHITE,
        collection,
        bevel=0.025,
    )
    parent(upper, root)

    underside = cube(
        "Upper_Cantilevered_Box_Gray_Underside",
        (-2.3, -0.15, 3.42),
        (10.7, 5.15, 0.18),
        MAT_UNDERSIDE,
        collection,
        bevel=0.01,
    )
    parent(underside, root)

    # Big dark glass panel on the front of the upper box.
    add_black_upper_glass(
        collection,
        root,
        x=-3.45,
        y=-2.70,
        z=5.55,
        width=6.9,
        height=1.95,
    )

    # White frame thickness around upper block front.
    upper_front_top = cube(
        "Upper_Front_Thick_White_Top_Frame",
        (-2.3, -2.79, 6.75),
        (10.7, 0.25, 0.36),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    upper_front_bottom = cube(
        "Upper_Front_Thick_White_Bottom_Frame",
        (-2.3, -2.79, 3.56),
        (10.7, 0.25, 0.28),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    upper_front_left = cube(
        "Upper_Front_Thick_White_Left_Frame",
        (-7.67, -2.79, 5.15),
        (0.32, 0.25, 3.0),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    upper_front_right = cube(
        "Upper_Front_Thick_White_Right_Frame",
        (3.05, -2.79, 5.15),
        (0.32, 0.25, 3.0),
        MAT_WHITE,
        collection,
        bevel=0.01,
    )
    for obj in [upper_front_top, upper_front_bottom, upper_front_left, upper_front_right]:
        parent(obj, root)

    # Side small windows on the upper white mass.
    add_top_side_windows(
        collection,
        root,
        side_x=3.02,
        start_y=-1.85,
        end_y=2.15,
        z_base=4.37,
    )

    # Small side windows on the front-right part of the upper block.
    add_randomized_small_windows(
        "Upper_Front_Right_Small_Window_Field",
        collection,
        root,
        start_x=-0.10,
        end_x=2.65,
        y=-2.72,
        z_base=4.35,
        rows=4,
        cols=7,
        row_spacing=0.52,
        col_spacing=0.42,
        width=0.16,
        height=0.24,
        pattern_seed=5,
    )

    # Middle recessed pilotis between the lower bar and upper box.
    add_recessed_pilotis(
        collection,
        root,
        x=-2.35,
        y=-2.18,
        z=3.02,
        width=5.8,
        height=1.10,
    )

    # Fine vertical white slats below the cantilever.
    for i in range(26):
        x = -7.0 + i * 0.35
        if i % 7 == 0:
            continue
        slat = cube(
            f"Under_Cantilever_White_Vertical_Slat_{i:02d}",
            (x, -2.26, 3.05),
            (0.055, 0.16, 0.92),
            MAT_FRAME,
            collection,
        )
        parent(slat, root)

    add_under_cantilever_columns(
        collection,
        root,
        x_start=-6.8,
        x_end=0.9,
        y=-1.90,
        z_base=1.95,
        height=1.42,
        count=9,
    )

    # Back/side long white extension, visible as the long rectangular body.
    rear_extension = cube(
        "Rear_Long_White_Extension_Mass",
        (5.5, 1.7, 2.20),
        (10.5, 3.5, 2.35),
        MAT_WHITE_SIDE,
        collection,
        bevel=0.02,
    )
    parent(rear_extension, root)

    for row in range(4):
        add_side_window_strip(
            f"Rear_Extension_Right_Side_Window_Row_{row:02d}",
            collection,
            root,
            x=10.78,
            start_y=0.25,
            end_y=3.05,
            z=1.28 + row * 0.43,
            count=13,
            width=0.16,
            height=0.24,
            pattern_offset=row,
        )

    # Roof caps and parapet outlines.
    roof_lower = cube(
        "Lower_Bar_Flat_Roof_Surface",
        (0.0, 0.0, 2.75),
        (24.7, 4.35, 0.12),
        MAT_ROOF,
        collection,
        bevel=0.01,
    )
    parent(roof_lower, root)

    roof_upper = cube(
        "Upper_Box_Flat_Roof_Surface",
        (-2.3, -0.15, 6.68),
        (10.6, 5.12, 0.14),
        MAT_ROOF,
        collection,
        bevel=0.01,
    )
    parent(roof_upper, root)

    add_parapet_and_edges(
        collection,
        root,
        "Lower_Bar_Thin_Parapet",
        (0.0, 0.0, 2.93),
        (24.8, 4.42, 0.22),
    )
    add_parapet_and_edges(
        collection,
        root,
        "Upper_Box_Thin_Parapet",
        (-2.3, -0.15, 6.88),
        (10.7, 5.22, 0.24),
    )

    # The reference has a sharp white front nose on the lower building.
    front_lip = cube(
        "Lower_Bar_Sharp_White_Front_Lip",
        (0.0, -2.33, 2.42),
        (24.7, 0.18, 0.28),
        MAT_WHITE,
        collection,
        bevel=0.008,
    )
    parent(front_lip, root)

    # Extra dense small-window strip near the lower roof line.
    add_window_strip(
        "Lower_Roofline_Tiny_Window_Strip",
        collection,
        root,
        start_x=-11.0,
        end_x=10.2,
        y=-2.34,
        z=2.25,
        count=54,
        width=0.16,
        height=0.22,
        pattern_offset=4,
    )

    return root, collection


def add_preview_camera_and_lights(collection):
    bpy.ops.object.light_add(type="SUN", location=(-4, -7, 9))
    sun = bpy.context.object
    sun.name = "Preview_Sun"
    sun.rotation_euler = (math.radians(52), 0, math.radians(-28))
    sun.data.energy = 2.2
    link_to(collection, sun)

    bpy.ops.object.light_add(type="AREA", location=(1, -8, 5))
    area = bpy.context.object
    area.name = "Preview_Front_Area_Light"
    area.data.energy = 360
    area.data.size = 6.0
    link_to(collection, area)

    bpy.ops.object.camera_add(
        location=(11.5, -13.0, 6.0),
        rotation=(math.radians(63), 0, math.radians(41)),
    )
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 34
    bpy.context.scene.camera = camera
    link_to(collection, camera)


def export_fbx(root):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)

    for child in root.children_recursive:
        child.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
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
    root, collection = create_reference_building()
    add_preview_camera_and_lights(collection)

    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    if hasattr(bpy.context.scene, "cycles"):
        bpy.context.scene.render.engine = "CYCLES"
        bpy.context.scene.cycles.samples = 64

    if EXPORT_FBX:
        export_fbx(root)


main()
