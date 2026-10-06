Popup3DButton - Readme

Overview
Popup3DButton is a small runtime helper script used by WindowHandlePopup.

It is responsible for:
- button hover detection
- button pressed state handling
- forwarding button click actions back to WindowHandlePopup

Pipeline note:
This helper is used by the runtime-generated 3D menu that is intended to run with the package's URP-based material setup.

In normal use, you do not manually configure this script.

Where this script is used
Popup3DButton is added automatically at runtime to the generated 3D menu buttons created by WindowHandlePopup.

This means:
- you do not need to place it on your prefabs manually
- you do not need to assign values in its Inspector for normal use

Important
This script still needs to exist somewhere inside your project so Unity can compile and use it.
Usually, it simply stays inside your Scripts folder.

What it does
When WindowHandlePopup generates the 3D menu,
each generated button receives a Popup3DButton component.

That component manages:
- whether the button is currently hovered
- whether the button is currently pressed
- which action should be executed when clicked

Examples of actions:
- open turn state
- open tilt state
- open slide state
- close the window
- hide the popup menu

Do I need to attach this script myself?
In normal usage:
No.

You usually only need:
- WindowHandlePopup on the animated handle object
- WindowInteractionBridge on the player

Popup3DButton will then be used automatically by the generated runtime menu.

When would I touch this script manually?
Only if you want to modify the internal behavior of the generated popup buttons.

For example:
- custom hover behavior
- custom click behavior
- custom button visual logic

For standard asset pack usage, no manual setup is required.

Common mistakes
- trying to place Popup3DButton manually on a window prefab
- expecting it to work on its own without WindowHandlePopup
- thinking it replaces the bridge or the popup script

Related scripts
- WindowHandlePopup
- WindowInteractionBridge
