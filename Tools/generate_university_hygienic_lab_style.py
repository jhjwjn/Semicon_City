import bpy
import math


# University Hygienic Laboratory / OPN Architects style generator.
#
# Goal:
# - Stable, buildable lab architecture for Unity.
# - Long floating laboratory bar over a transparent ground level.
# - Repeating horizontal ribbon windows.
# - Slender ground-floor columns.
# - Two-story glazed entry volume.
# - Gray circulation/service core.
# - No rooftop ventilation equipment.
#
# Blender:
# Scripting > Text > Open > Run Script
#
# Unity:
# Set EXPORT_FBX = True to export to Assets/Models.


EXPORT_FBX = False
FBX_PATH = "/Users/jinyeong/Desktop/SemiconCity/Assets/Models/University_Hygienic_Lab_Style.fbx"


MAT_LAB_BAR = None
MAT_LIMESTONE = None
MAT_GLASS = None
MAT_DARK_GLASS = None
MAT_FRAME = None
MAT_COLUMN = None
MAT_CORE = None
MAT_ROOF = None
MAT_SHADOW = None
MAT_FLOOR = None


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


def make_material(name, color, roughness=0.55, metallic=0.0, alpha=1.0):
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


def init_materials():
    global MAT_LAB_BAR
    global MAT_LIMESTONE
    global MAT_GLASS
    global MAT_DARK_GLASS
    global MAT_FRAME
    global MAT_COLUMN
    global MAT_CORE
    global MAT_ROOF
    global MAT_SHADOW
    global MAT_FLOOR

    MAT_LAB_BAR = make_material(
        "UNITY_ASSIGN_Light_Lab_Bar_Cladding",
        (0.80, 0.75, 0.64, 1.0),
        roughness=0.72,
    )
    MAT_LIMESTONE = make_material(
        "UNITY_ASSIGN_Limestone_Accent",
        (0.86, 0.84, 0.78, 1.0),
        roughness=0.70,
    )
    MAT_GLASS = make_material(
        "UNITY_ASSIGN_Clear_Glass",
        (0.50, 0.58, 0.60, 0.42),
        roughness=0.16,
        alpha=0.42,
    )
    MAT_DARK_GLASS = make_material(
        "UNITY_ASSIGN_Dark_Glass_Backplate",
        (0.06, 0.08, 0.09, 1.0),
        roughness=0.38,
    )
    MAT_FRAME = make_material(
        "UNITY_ASSIGN_Aluminum_Window_Frame",
        (0.76, 0.77, 0.75, 1.0),
        roughness=0.34,
        metallic=0.18,
    )
    MAT_COLUMN = make_material(
        "UNITY_ASSIGN_Slender_Column",
        (0.78, 0.79, 0.77, 1.0),
        roughness=0.48,
        metallic=0.08,
    )
    MAT_CORE = make_material(
        "UNITY_ASSIGN_Gray_Service_Core",
        (0.40, 0.42, 0.42, 1.0),
        roughness=0.64,
    )
    MAT_ROOF = make_material(
        "UNITY_ASSIGN_Roof_Parapet",
        (0.62, 0.64, 0.62, 1.0),
        roughness=0.68,
    )
    MAT_SHADOW = make_material(
        "UNITY_ASSIGN_Interior_Shadow",
        (0.035, 0.037, 0.038, 1.0),
        roughness=0.60,
    )
    MAT_FLOOR = make_material(
        "UNITY_ASSIGN_Concrete_Floor_Slab",
        (0.70, 0.70, 0.68, 1.0),
        roughness=0.68,
    )


def cube(name, loc, scale, mat, collection, bevel=0.0, segments=1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    if mat:
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


def empty(name, collection):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "CUBE"
    obj.empty_display_size = 1.0
    collection.objects.link(obj)
    return obj


def add_window_panel(
    name,
    collection,
    root,
    loc,
    scale,
    axis="Y",
    frame=True,
):
    x, y, z = loc
    sx, sy, sz = scale

    panel = cube(name, loc, scale, MAT_GLASS, collection, bevel=0.004)
    parent(panel, root)

    if not frame:
        return

    if axis == "Y":
        top = cube(
            f"{name}_Frame_Top",
            (x, y - 0.018, z + sz * 0.5 + 0.018),
            (sx + 0.04, sy * 1.35, 0.035),
            MAT_FRAME,
            collection,
        )
        bottom = cube(
            f"{name}_Frame_Bottom",
            (x, y - 0.018, z - sz * 0.5 - 0.018),
            (sx + 0.04, sy * 1.35, 0.035),
            MAT_FRAME,
            collection,
        )
        left = cube(
            f"{name}_Frame_Left",
            (x - sx * 0.5 - 0.018, y - 0.018, z),
            (0.035, sy * 1.35, sz + 0.07),
            MAT_FRAME,
            collection,
        )
        right = cube(
            f"{name}_Frame_Right",
            (x + sx * 0.5 + 0.018, y - 0.018, z),
            (0.035, sy * 1.35, sz + 0.07),
            MAT_FRAME,
            collection,
        )
    else:
        top = cube(
            f"{name}_Frame_Top",
            (x + 0.018, y, z + sz * 0.5 + 0.018),
            (sx * 1.35, sy + 0.04, 0.035),
            MAT_FRAME,
            collection,
        )
        bottom = cube(
            f"{name}_Frame_Bottom",
            (x + 0.018, y, z - sz * 0.5 - 0.018),
            (sx * 1.35, sy + 0.04, 0.035),
            MAT_FRAME,
            collection,
        )
        left = cube(
            f"{name}_Frame_Left",
            (x + 0.018, y - sy * 0.5 - 0.018, z),
            (sx * 1.35, 0.035, sz + 0.07),
            MAT_FRAME,
            collection,
        )
        right = cube(
            f"{name}_Frame_Right",
            (x + 0.018, y + sy * 0.5 + 0.018, z),
            (sx * 1.35, 0.035, sz + 0.07),
            MAT_FRAME,
            collection,
        )

    for obj in [top, bottom, left, right]:
        parent(obj, root)


def add_ribbon_window_front(
    name,
    collection,
    root,
    x_center,
    y,
    z,
    width,
    height,
    count,
):
    dark_back = cube(
        f"{name}_Continuous_Dark_Backplate",
        (x_center, y + 0.045, z),
        (width + 0.16, 0.06, height + 0.16),
        MAT_DARK_GLASS,
        collection,
    )
    parent(dark_back, root)

    bay_w = width / count

    for i in range(count):
        x = x_center - width * 0.5 + bay_w * (i + 0.5)
        panel_width = bay_w * 0.78
        if i % 6 == 2:
            panel_width = bay_w * 0.55
        add_window_panel(
            f"{name}_Panel_{i:02d}",
            collection,
            root,
            (x, y - 0.025, z),
            (panel_width, 0.045, height * 0.82),
            axis="Y",
            frame=True,
        )

    for i in range(count + 1):
        x = x_center - width * 0.5 + bay_w * i
        mullion = cube(
            f"{name}_Major_Mullion_{i:02d}",
            (x, y - 0.052, z),
            (0.055, 0.075, height + 0.12),
            MAT_FRAME,
            collection,
        )
        parent(mullion, root)


def add_ribbon_window_side(
    name,
    collection,
    root,
    x,
    y_center,
    z,
    length,
    height,
    count,
):
    dark_back = cube(
        f"{name}_Continuous_Dark_Backplate",
        (x - 0.045, y_center, z),
        (0.06, length + 0.16, height + 0.16),
        MAT_DARK_GLASS,
        collection,
    )
    parent(dark_back, root)

    bay_w = length / count

    for i in range(count):
        y = y_center - length * 0.5 + bay_w * (i + 0.5)
        panel_length = bay_w * 0.76
        add_window_panel(
            f"{name}_Panel_{i:02d}",
            collection,
            root,
            (x - 0.065, y, z),
            (0.045, panel_length, height * 0.82),
            axis="X",
            frame=True,
        )

    for i in range(count + 1):
        y = y_center - length * 0.5 + bay_w * i
        mullion = cube(
            f"{name}_Major_Mullion_{i:02d}",
            (x - 0.085, y, z),
            (0.075, 0.055, height + 0.12),
            MAT_FRAME,
            collection,
        )
        parent(mullion, root)


def add_ground_floor_glass(collection, root):
    # Transparent first floor under the floating lab bar.
    width = 23.8
    y_front = -2.58
    z = 1.05
    height = 1.72
    count = 18
    bay_w = width / count

    backplate = cube(
        "Ground_Floor_Front_Glass_Backplate",
        (0.0, y_front + 0.05, z),
        (width + 0.2, 0.06, height + 0.18),
        MAT_DARK_GLASS,
        collection,
    )
    parent(backplate, root)

    for i in range(count):
        x = -width * 0.5 + bay_w * (i + 0.5)
        add_window_panel(
            f"Ground_Floor_Front_Glass_Bay_{i:02d}",
            collection,
            root,
            (x, y_front - 0.03, z),
            (bay_w * 0.84, 0.045, height * 0.86),
            axis="Y",
            frame=True,
        )

    for i in range(count + 1):
        x = -width * 0.5 + bay_w * i
        mullion = cube(
            f"Ground_Floor_Front_Heavy_Mullion_{i:02d}",
            (x, y_front - 0.07, z),
            (0.075, 0.09, height + 0.12),
            MAT_FRAME,
            collection,
        )
        parent(mullion, root)


def add_columns(collection, root):
    count = 13
    width = 21.5
    for i in range(count):
        x = -width * 0.5 + width * i / (count - 1)
        col = cube(
            f"Ground_Floor_Slender_Column_{i:02d}",
            (x, -2.05, 1.05),
            (0.14, 0.14, 2.1),
            MAT_COLUMN,
            collection,
            bevel=0.018,
        )
        parent(col, root)

        base = cube(
            f"Ground_Floor_Column_Base_{i:02d}",
            (x, -2.05, 0.08),
            (0.32, 0.32, 0.16),
            MAT_COLUMN,
            collection,
            bevel=0.018,
        )
        parent(base, root)


def add_entry_atrium(collection, root):
    # Two-story public entrance with tall glass wall.
    atrium = cube(
        "North_Public_Entry_Glass_Atrium_Backplate",
        (-8.9, -2.76, 1.75),
        (3.45, 0.10, 3.30),
        MAT_DARK_GLASS,
        collection,
        bevel=0.006,
    )
    parent(atrium, root)

    cols = 4
    rows = 3
    width = 3.2
    height = 3.05
    panel_w = width / cols
    panel_h = height / rows

    for r in range(rows):
        for c in range(cols):
            x = -8.9 - width * 0.5 + panel_w * (c + 0.5)
            z = 1.75 - height * 0.5 + panel_h * (r + 0.5)
            add_window_panel(
                f"North_Public_Entry_Atrium_Glass_R{r:02d}_C{c:02d}",
                collection,
                root,
                (x, -2.84, z),
                (panel_w * 0.88, 0.045, panel_h * 0.86),
                axis="Y",
                frame=False,
            )

    for c in range(cols + 1):
        x = -8.9 - width * 0.5 + panel_w * c
        mullion = cube(
            f"North_Public_Entry_Atrium_Mullion_V_{c:02d}",
            (x, -2.88, 1.75),
            (0.06, 0.08, height + 0.16),
            MAT_FRAME,
            collection,
        )
        parent(mullion, root)

    for r in range(rows + 1):
        z = 1.75 - height * 0.5 + panel_h * r
        mullion = cube(
            f"North_Public_Entry_Atrium_Mullion_H_{r:02d}",
            (-8.9, -2.89, z),
            (width + 0.18, 0.08, 0.055),
            MAT_FRAME,
            collection,
        )
        parent(mullion, root)

    frame_top = cube(
        "North_Public_Entry_White_Aluminum_Top_Frame",
        (-8.9, -2.94, 3.48),
        (3.7, 0.28, 0.28),
        MAT_LIMESTONE,
        collection,
        bevel=0.012,
    )
    frame_left = cube(
        "North_Public_Entry_White_Aluminum_Left_Frame",
        (-10.72, -2.94, 1.75),
        (0.28, 0.28, 3.35),
        MAT_LIMESTONE,
        collection,
        bevel=0.012,
    )
    frame_right = cube(
        "North_Public_Entry_White_Aluminum_Right_Frame",
        (-7.08, -2.94, 1.75),
        (0.28, 0.28, 3.35),
        MAT_LIMESTONE,
        collection,
        bevel=0.012,
    )

    for obj in [frame_top, frame_left, frame_right]:
        parent(obj, root)


def add_service_core(collection, root):
    core = cube(
        "Gray_Service_Core_Vertical_Mass",
        (11.6, 0.45, 2.45),
        (2.2, 4.9, 4.9),
        MAT_CORE,
        collection,
        bevel=0.018,
    )
    parent(core, root)

    for row in range(4):
        for col in range(2):
            z = 0.9 + row * 0.82
            y = -1.20 + col * 0.75
            add_window_panel(
                f"Service_Core_Narrow_Window_R{row:02d}_C{col:02d}",
                collection,
                root,
                (10.46, y, z),
                (0.045, 0.26, 0.42),
                axis="X",
                frame=True,
            )


def add_lab_bar(collection, root):
    # Floating second-floor lab bar.
    bar = cube(
        "Floating_Second_Floor_Lab_Bar",
        (0.0, 0.0, 3.15),
        (24.8, 4.55, 2.05),
        MAT_LAB_BAR,
        collection,
        bevel=0.022,
    )
    parent(bar, root)

    lower_limestone_band = cube(
        "Lab_Bar_Lower_Limestone_Band",
        (0.0, -2.34, 2.42),
        (25.0, 0.22, 0.32),
        MAT_LIMESTONE,
        collection,
        bevel=0.008,
    )
    upper_limestone_band = cube(
        "Lab_Bar_Upper_Limestone_Band",
        (0.0, -2.34, 3.92),
        (25.0, 0.22, 0.25),
        MAT_LIMESTONE,
        collection,
        bevel=0.008,
    )
    parent(lower_limestone_band, root)
    parent(upper_limestone_band, root)

    add_ribbon_window_front(
        "Second_Floor_Main_Ribbon_Window",
        collection,
        root,
        x_center=1.4,
        y=-2.52,
        z=3.22,
        width=18.8,
        height=0.76,
        count=22,
    )

    # Secondary smaller upper ribbon, broken up for the long elevation.
    add_ribbon_window_front(
        "Second_Floor_Upper_Narrow_Ribbon_Window",
        collection,
        root,
        x_center=2.0,
        y=-2.55,
        z=3.72,
        width=15.2,
        height=0.34,
        count=18,
    )

    # Side ribbon on right end of the bar.
    add_ribbon_window_side(
        "East_End_Lab_Bar_Ribbon_Window",
        collection,
        root,
        x=12.52,
        y_center=-0.2,
        z=3.20,
        length=3.25,
        height=0.72,
        count=5,
    )


def add_roof_and_floor_slabs(collection, root):
    ground_slab = cube(
        "Ground_Level_Concrete_Slab",
        (0.0, -0.15, -0.04),
        (26.0, 5.9, 0.08),
        MAT_FLOOR,
        collection,
        bevel=0.012,
    )
    parent(ground_slab, root)

    floor_plate = cube(
        "Second_Floor_Floating_Slab_Line",
        (0.0, -0.08, 2.12),
        (25.4, 4.85, 0.15),
        MAT_FLOOR,
        collection,
        bevel=0.008,
    )
    parent(floor_plate, root)

    roof = cube(
        "Flat_Roof_No_Mechanical_Equipment",
        (0.0, 0.0, 4.24),
        (25.1, 4.75, 0.16),
        MAT_ROOF,
        collection,
        bevel=0.012,
    )
    parent(roof, root)

    parapet_front = cube(
        "Roof_Thin_Front_Parapet",
        (0.0, -2.43, 4.45),
        (25.3, 0.20, 0.38),
        MAT_ROOF,
        collection,
        bevel=0.008,
    )
    parapet_back = cube(
        "Roof_Thin_Back_Parapet",
        (0.0, 2.43, 4.45),
        (25.3, 0.20, 0.38),
        MAT_ROOF,
        collection,
        bevel=0.008,
    )
    parapet_left = cube(
        "Roof_Thin_Left_Parapet",
        (-12.65, 0.0, 4.45),
        (0.20, 4.9, 0.38),
        MAT_ROOF,
        collection,
        bevel=0.008,
    )
    parapet_right = cube(
        "Roof_Thin_Right_Parapet",
        (12.65, 0.0, 4.45),
        (0.20, 4.9, 0.38),
        MAT_ROOF,
        collection,
        bevel=0.008,
    )

    for obj in [parapet_front, parapet_back, parapet_left, parapet_right]:
        parent(obj, root)


def add_small_entries(collection, root):
    # Modest employee entrance opposite the main atrium, without over-designing.
    canopy = cube(
        "Employee_Entrance_Simple_Canopy",
        (7.8, -2.92, 2.05),
        (2.1, 0.9, 0.14),
        MAT_LIMESTONE,
        collection,
        bevel=0.012,
    )
    parent(canopy, root)

    door_back = cube(
        "Employee_Entrance_Dark_Backplate",
        (7.8, -2.66, 0.93),
        (1.45, 0.08, 1.28),
        MAT_DARK_GLASS,
        collection,
    )
    parent(door_back, root)

    add_window_panel(
        "Employee_Entrance_Glass_Door_Left",
        collection,
        root,
        (7.45, -2.74, 0.92),
        (0.52, 0.045, 1.16),
        axis="Y",
        frame=True,
    )
    add_window_panel(
        "Employee_Entrance_Glass_Door_Right",
        collection,
        root,
        (8.15, -2.74, 0.92),
        (0.52, 0.045, 1.16),
        axis="Y",
        frame=True,
    )


def add_minimal_context_scale(collection, root):
    # Very small base only, not a park/road/site model.
    apron = cube(
        "Building_Only_Minimal_Plaza_Base",
        (0.0, -3.45, -0.015),
        (27.0, 1.4, 0.03),
        MAT_FLOOR,
        collection,
        bevel=0.006,
    )
    parent(apron, root)

    for i in range(9):
        joint = cube(
            f"Minimal_Plaza_Joint_{i:02d}",
            (-12.0 + i * 3.0, -3.45, 0.01),
            (0.025, 1.35, 0.012),
            MAT_LIMESTONE,
            collection,
        )
        parent(joint, root)


def create_building():
    collection = make_collection("University_Hygienic_Lab_Style")
    root = empty("University_Hygienic_Lab_Style_Root", collection)

    add_roof_and_floor_slabs(collection, root)
    add_ground_floor_glass(collection, root)
    add_columns(collection, root)
    add_lab_bar(collection, root)
    add_entry_atrium(collection, root)
    add_service_core(collection, root)
    add_small_entries(collection, root)
    add_minimal_context_scale(collection, root)

    return root, collection


def add_preview_camera_and_light(collection):
    bpy.ops.object.light_add(type="SUN", location=(-7.0, -8.0, 9.5))
    sun = bpy.context.object
    sun.name = "Preview_Sun"
    sun.rotation_euler = (math.radians(48), 0.0, math.radians(-30))
    sun.data.energy = 2.2
    link_to(collection, sun)

    bpy.ops.object.light_add(type="AREA", location=(0.0, -7.5, 4.2))
    area = bpy.context.object
    area.name = "Preview_Area_Light"
    area.data.energy = 300
    area.data.size = 6.0
    link_to(collection, area)

    bpy.ops.object.camera_add(
        location=(16.0, -12.0, 6.2),
        rotation=(math.radians(62), 0.0, math.radians(48)),
    )
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 35
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

    root, collection = create_building()
    add_preview_camera_and_light(collection)

    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    if EXPORT_FBX:
        export_fbx(root)
dhdn 

main()
