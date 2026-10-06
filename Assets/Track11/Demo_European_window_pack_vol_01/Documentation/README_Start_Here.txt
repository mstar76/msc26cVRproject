README - Start Here

Thank you for purchasing this asset pack.

This package contains a collection of European-style window and balcony door models for Unity, including static variants, animated variants, and supporting scripts for interaction and visual customization.

Please read this file first before setting up the included scripts.

Package Overview
This asset pack includes:
- 29 static frame-only window variants
- 28 animated windows
- 4 animated balcony doors
- supporting interaction scripts
- a visual customization tool
- demo and utility content, depending on your imported folders

Pipeline Compatibility
This package uses Universal Render Pipeline materials.
Please import this asset pack into a URP project for the intended rendering result.

If you plan to use the assets in HDRP or in another rendering setup, additional material and shader adjustment will be required.

Main Included Scripts
This pack includes the following important scripts:

- WindowHandlePopup
Main interaction and animation script for animated window and door handles.
This script generates the 3D interaction menu and controls window states such as turn, tilt, slide, and close.

- WindowInteractionBridge
Player-side bridge script used to detect and trigger interactive windows through raycasting.

- Popup3DButton
Runtime helper script used automatically by the generated 3D popup menu.

- ColorChooser3D
Visual customization tool for overriding selected material properties directly in the Inspector.

- ColorChooser3DEditor
Custom editor companion script required by ColorChooser3D.
This script must be placed inside an folder called "Editor".

Documentation
Detailed setup instructions for the main scripts can be found in the Documentation folder.

Recommended reading order:
- README_Start_Here
- WindowHandlePopup_Readme
- WindowInteractionBridge_Readme
- Popup3DButton_Readme
- ColorChooser3D_Readme

Basic Setup for Interactive Windows
To use animated and interactive windows correctly, the usual setup is:

1. Place an included animated prefab into your scene
2. The animated prefabs are already prepared with WindowHandlePopup, colliders, and the required interaction setup
3. Attach WindowInteractionBridge to your player or controller object
4. Make sure the player setup uses a camera for forward interaction raycasts
5. Test the interaction in play mode

Important:
If you use the included animated prefabs, you do not need to rebuild the WindowHandlePopup setup from scratch.
That setup is already included on those prefabs.

If you create your own custom animated model and want to use the same interaction system,
you will need to assign WindowHandlePopup, colliders, pivots, and the related settings manually.

Important:
WindowHandlePopup should normally be attached to the interactive handle object, not to the overall prefab root, unless the root itself is the interactive handle.

Basic Setup for Visual Customization
To use ColorChooser3D:

1. Attach ColorChooser3D to the main root object of the model or prefab you want to customize
2. Make sure ColorChooser3DEditor exists inside an Editor folder
3. Open the Inspector and let the script collect the renderer and material entries
4. Use Main Materials mode or Materials by Variants mode depending on the level of detail you need

Important:
ColorChooser3D should be attached to the full object root you want the script to scan, not to the player, camera, or an unrelated scene object.

Version Information
All included scripts are currently version 0.1.

This means the tools are functional, but some parts of the workflow may still evolve over time.

Current Work In Progress Areas
The following systems should currently be treated as work in progress:
- French window logic inside WindowHandlePopup
- slide logic inside WindowHandlePopup

Please test these setups carefully if you use them in production scenes.

Important Notes
- Popup3DButton is used automatically at runtime and usually does not require manual setup
- ColorChooser3DEditor must be inside an Editor folder or its Inspector features will not work
- Apply To All behavior in ColorChooser3D works only inside the current scene
- TXT preset export and import in ColorChooser3D can be used to move visual setups between scenes
- static frame-only windows do not require the interaction system unless you intentionally want to build your own presentation logic
- this package uses URP materials and should be imported into a URP project

Recommended Folder Usage
If you reorganize the package after import, do so carefully.
Editor scripts should remain inside an Editor folder.
Runtime scripts should remain accessible to Unity's normal compilation flow.

Support
This is my first asset pack, and a great deal of time and care went into it.

If you encounter a problem, incorrect setup behavior, or an issue with a model, please refer to the corresponding readme first.
If the issue is still unclear, please contact me with a short description of:
- which asset you are using
- which script is involved
- what your hierarchy looks like
- what behavior you expected
- what behavior you are getting instead

This makes support much easier and faster.

Thank you very much, and I hope this pack is useful for your project.
