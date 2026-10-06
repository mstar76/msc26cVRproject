WindowHandlePopup - Readme

Version
All scripts in this package are currently version 0.1.

Overview
WindowHandlePopup is the main interaction script for animated windows and balcony doors.
It is responsible for:
- opening and closing the generated 3D interaction menu
- playing the animation states such as turn, tilt, slide, and close
- handling optional French window logic
- handling optional audio playback
- generating the 3D button menu during runtime

Pipeline note:
The included package materials are intended for URP projects.
The generated runtime menu is also expected to be used inside a URP setup.

Where to attach this script
Attach WindowHandlePopup directly to the handle object of the animated window or door.

Important:
Do not place this script on the prefab root unless the root itself is the actual interactive handle object.
In most cases, the correct object is the mesh object that represents the handle.

Typical hierarchy example:
Window_ROOT
- mechanic_PIVOT
- sash_tilt_PIVOT
-- sash_turn_PIVOT
--- handle
---- handle_bar_PIVOT

In this example, WindowHandlePopup should usually be attached to:
- handle_bar_PIVOT

Required setup
This script requires:
- a collider on the same object, or on the interactive handle object used for raycast interaction
- the correct pivot references assigned in the Inspector
- a player-side interaction bridge, if you want to trigger it through a first-person controller

The collider is important because the player interaction system needs something to hit with a raycast.

What this script controls
Depending on how you configure it, WindowHandlePopup can control:
- turn opening
- tilt opening
- slide opening
- close state
- handle rotation
- mechanic pivot rotation
- optional French window marker logic
- optional 3D menu generation
- optional audio playback

Inspector setup
The most important part of the setup is assigning the correct transforms.

Typical transform fields:
- turn pivot
- tilt pivot
- handle bar pivot
- mechanic pivot
- slide pivot
- lift pivot

Only assign the pivots you actually need for the current window type.

Important:
For turn and tilt setups, the mechanic pivot is not optional in normal use.
If you configure turn logic, you should also assign the mechanic pivot for the turn movement.
If you configure tilt logic, you should also assign the mechanic pivot for the turn movement because even if its tilt, the mechanic turns.
Turn and tilt are designed to work together with the mechanic rotation, not as completely separate systems.

Examples:
- A turn-only window usually needs a turn pivot, a handle bar pivot, and a mechanic pivot
- A tilt-only window usually needs a tilt pivot, a handle bar pivot, and a mechanic pivot
- A turn-and-tilt window needs both turn and tilt pivots, the handle pivot, and the mechanic pivot
- A sliding element needs slide-related pivots
- A static object should not use this script

Button generation logic
The 3D menu is generated at runtime.
Buttons such as Turn, Tilt, Slide, or Close are only created if the script detects that the corresponding functionality is available.

In practice, this means:
- if a required pivot is missing, that button will not be created
- if the corresponding open angle or movement value is effectively disabled, that button will not be created

This helps prevent menu entries from appearing for features that do not exist on the current object.

You can also set initial states for turn / open / slided.

3D menu placement
The script creates a 3D menu near the handle during gameplay.
You can configure:
- menu size
- button size
- text
- colors
- title
- offsets
- camera-facing behavior
- menu rotation offset

If the menu appears flipped, use the menu rotation offset or the camera-facing settings to correct it.

Animation setup
The script supports different movement types depending on the assigned pivots and values.

Examples:
- Turn uses rotational movement on the turn pivot and also drives the mechanic pivot
- Tilt uses rotational movement on the tilt pivot and also drives the mechanic pivot
- Slide uses positional movement on the slide pivot
- Lift uses positional movement on the lift pivot
- Handle rotation is controlled separately through the handle bar pivot

Make sure the pivots are already correctly positioned and oriented in your model before assigning them.
The script does not fix incorrectly built hierarchies.

Audio support
WindowHandlePopup supports optional audio clips for:
- handle movement
- frame opening
- frame closing

If you want audio:
- enable audio in the Inspector
- assign the audio clip references
- assign the handle and frame sound anchors if needed

The sound anchor determines where the sound is played from in world space.

French window support (Work in progress)
The script also supports French window logic using marker-based state checks.
This allows one wing to depend on the state of another wing.

Important:
French window logic should currently be treated as work in progress.
Use this only if your model is built specifically for this type of setup and you have tested the full interaction flow carefully.

Slide support (Work in progress)
The script also supports slide-based setups with optional lift logic, depending on the configured pivots.

Important:
Slide logic should currently be treated as work in progress.
Use it only on models that were specifically built and tested for this kind of movement behavior.

Common mistakes
- attaching the script to the prefab root instead of the actual handle
- forgetting to add or enable a collider on the interactive object
- assigning the wrong pivot transforms
- assigning pivots that are not actually parented correctly in the model
- configuring turn or tilt without also assigning the mechanic pivot
- expecting buttons to appear for functions that are not configured
- using the script on static frame-only models

Recommended usage
Use WindowHandlePopup only on animated, interactive window or door elements.
Do not use it on static frame-only objects.

For static objects, no interaction script is required unless you are building a custom presentation or showroom scene.

Related scripts
- WindowInteractionBridge
- Popup3DButton
