ColorChooser3D - Readme

Overview
ColorChooser3D is a visual customization tool for 3D objects in Unity.
It allows you to override the appearance of supported materials directly in the Inspector without permanently editing the original shared materials.

The tool works together with ColorChooser3DEditor.

Pipeline note:
This package version is set up for URP materials.
For the intended result, use this tool inside a URP project.

Important:
ColorChooser3D is not limited to this window pack.
It can also be used on many other 3D assets if they use supported materials and renderers.

Where to attach this script
Attach ColorChooser3D to the main root object of the specific model or prefab you want to customize.

This usually means:
- the top-level root of the placed prefab instance in the scene
- or the top-level root object of the model hierarchy

Correct examples:
- the root of a window prefab placed in the scene
- the root of a door prefab
- the root object of another mesh-based object you want to customize

Incorrect examples:
- the player object
- the camera
- a random empty scene object with no relation to the model
- only one deep child mesh when you actually want the whole object to be managed together

Practical rule:
Attach ColorChooser3D to the object that contains the full renderer hierarchy you want the script to scan.

If your prefab looks like this:
Window_ROOT
- frame
- sash
- handle
- glass

Then ColorChooser3D should normally go on:
- Window_ROOT

Not on:
- frame
- sash
- handle
unless you intentionally want to customize only that isolated part.

What the script does
Once attached, the script scans the renderers inside that object hierarchy and creates editable entries for the detected materials.

You can then override selected visual properties directly in the Inspector.

Supported override types
Depending on the material and shader setup, the tool supports:
- color
- alpha
- metallic
- smoothness
- specular color
- emission color
- normal strength
- occlusion strength

These overrides are applied through MaterialPropertyBlock,
which means the original shared material asset is not permanently rewritten.

The exact visible result of an override depends on whether the active material shader exposes the corresponding property.

ColorChooser3DEditor requirement
ColorChooser3D works together with ColorChooser3DEditor.

Important:
The editor script must be placed somewhere inside a folder named:
- Editor

For example:
Assets/Track11/Editor/ColorChooser3DEditor.cs

If the editor script is not placed inside an Editor folder,
the custom Inspector will not work correctly.

Working modes
The tool has two main modes.

Main Materials mode
This mode groups the object by its main materials.
Use this mode when you want faster, broader changes across the object.

This is the best choice when:
- you want a quick global look adjustment
- multiple child meshes share the same material
- you do not need per-slot precision

Materials by Variants mode
This mode exposes more detailed material-slot-level control.

Use this mode when:
- different meshes using similar materials need different settings
- you want more precise visual control
- you need separate overrides for specific renderer and slot combinations

Apply To All behavior
If multiple objects in the same scene use ColorChooser3D,
you can copy override values from one object to other ColorChooser3D objects in that same scene.

Important:
This works only inside the currently open scene.

It is not project-wide and not automatically shared across newly created scenes.

TXT preset workflow
The tool supports TXT preset export and import.

This allows you to:
- save a visual setup from one scene
- reopen another scene later
- import the same setup again

This is useful when:
- you want to preserve a look between scenes
- you create multiple presentation scenes
- you want to reuse a color setup later

Pro Settings
Inside the Pro Settings area, you can:
- refresh the material list
- refresh the current view
- save a TXT preset
- load a TXT preset
- reset all ColorChooser3D values in the current scene back to stock

When to use this tool
Use ColorChooser3D when you want:
- fast visual iteration
- object-specific appearance changes
- multiple variants in one scene
- a non-destructive way to preview different material looks

Common mistakes
- attaching the script to the wrong object instead of the full model root
- attaching it to the player or camera
- expecting it to automatically affect objects that do not have the script
- forgetting to place ColorChooser3DEditor inside an Editor folder
- expecting Apply To All to work across scenes automatically
- expecting it to permanently rewrite the original material assets

Recommended usage
For most users, the best workflow is:
- place your prefab in the scene
- attach ColorChooser3D to the prefab root
- let the script collect the renderers
- start in Main Materials mode
- switch to Materials by Variants only if more precision is needed

Related scripts
- ColorChooser3DEditor
