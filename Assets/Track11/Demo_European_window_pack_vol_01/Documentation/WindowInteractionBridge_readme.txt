WindowInteractionBridge - Readme

Overview
WindowInteractionBridge connects the player interaction system to WindowHandlePopup.

This script allows the player to:
- look at a handle
- interact with it through raycasting
- open the 3D popup menu
- click the generated 3D menu buttons during gameplay

This script is designed to stay independent from a specific controller package.

Pipeline note:
This interaction script itself is controller-independent, but the package materials and demo setup are intended for URP projects.

Where to attach this script
Attach WindowInteractionBridge to the player object that performs the interaction raycast.

Usually this means:
- the player root
- the controller object
- or the camera holder object

Recommended setup:
Attach it to the same player object that already contains or references your camera.

Important:
This script is not meant for window prefabs.
Do not attach it to the window.
It belongs to the player side.

Required setup
This script needs:
- a valid camera reference, either assigned manually or discoverable from the player setup
- interactive window handle objects in the scene that use WindowHandlePopup
- colliders on those interactive objects

How it works
The script performs a forward raycast from the player view.
If the raycast hits an object with WindowHandlePopup, it can trigger the popup menu.

When the popup menu is open, the script can also interact with the generated 3D buttons.

This makes it possible to use:
- one player controller
- many different interactive windows
- without hardwiring the window logic into the controller itself

Why this script exists
The goal of the bridge is separation.

Instead of embedding all window interaction logic directly into the player controller,
the controller only needs a simple bridge that knows how to:
- detect a valid target
- open the popup
- interact with popup buttons

This keeps the window system reusable and easier to integrate into different projects.

Supported use cases
This script is useful when:
- you use your own first-person controller
- you use a third-party controller
- you want to keep the window logic separate from player movement logic

Common mistakes
- attaching the bridge to the window instead of the player
- forgetting to assign or detect the correct camera
- trying to use the bridge without colliders on the window handle objects
- expecting the bridge to animate windows by itself

Important note
WindowInteractionBridge does not animate the window directly.
It only handles interaction and forwards valid actions to WindowHandlePopup.

Related scripts
- WindowHandlePopup
- Popup3DButton
