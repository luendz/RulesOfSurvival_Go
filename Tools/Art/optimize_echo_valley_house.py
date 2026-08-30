"""Reduce a single Echo Valley house FBX without opening Unity.

Usage:
    blender --background --python optimize_echo_valley_house.py -- \
        input.fbx output.fbx 120000
"""

from __future__ import annotations

import json
import math
import pathlib
import sys

import bpy


def mesh_objects() -> list[bpy.types.Object]:
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def triangle_count(objects: list[bpy.types.Object]) -> int:
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        total += len(mesh.loop_triangles)
        evaluated.to_mesh_clear()
    return total


def apply_modifier(obj: bpy.types.Object, modifier_name: str) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier_name)


def optimize(input_path: pathlib.Path, output_path: pathlib.Path, target: int) -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(input_path))

    objects = mesh_objects()
    if not objects:
        raise RuntimeError(f"No mesh objects found in {input_path}")

    original_triangles = triangle_count(objects)
    ratio = min(1.0, max(0.001, target / max(1, original_triangles)))

    if ratio < 0.999:
        for obj in objects:
            modifier = obj.modifiers.new("ROS_EchoValley_Optimize", "DECIMATE")
            modifier.decimate_type = "COLLAPSE"
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            modifier.use_symmetry = False
            apply_modifier(obj, modifier.name)

    optimized_triangles = triangle_count(objects)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
    )

    print(
        "ROS_HOUSE_OPTIMIZATION="
        + json.dumps(
            {
                "input": str(input_path),
                "output": str(output_path),
                "objects": len(objects),
                "original_triangles": original_triangles,
                "optimized_triangles": optimized_triangles,
                "ratio": ratio,
                "target_triangles": target,
            },
            sort_keys=True,
        )
    )


def main() -> None:
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 3:
        raise SystemExit("Expected: input.fbx output.fbx target_triangles")

    input_path = pathlib.Path(arguments[0]).resolve()
    output_path = pathlib.Path(arguments[1]).resolve()
    target = int(arguments[2])
    if target < 10_000:
        raise SystemExit("target_triangles must be at least 10000")

    optimize(input_path, output_path, target)


if __name__ == "__main__":
    main()
