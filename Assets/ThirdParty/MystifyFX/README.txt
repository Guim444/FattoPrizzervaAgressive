***********************
*      MYSTIFY FX     *
* Created by Kronnect * 
*      README FILE    *
***********************


Notice about Universal Rendering Pipeline
-----------------------------------------
This package is designed for URP.
It requires Unity 2022.3 or later
To install the plugin correctly:

1) Make sure you have Universal Rendering Pipeline asset installed (from Package Manager).
2) Go to Project Settings / Graphics.
3) Double click the Universal Rendering Pipeline asset.
4) Double click the Renderer asset.
5) Click "+" to add the Mystify FX Renderer Feature to the list of the Renderer Features.

Note: URP assets can be assigned to Settings / Graphics and also Settings / Quality. Check both sections!

You can also find a sample Mystify Renderer asset in the demo scene.


Quick help: how to use this asset?
----------------------------------

Mystify FX is an all-in-one fx asset for creating all kind of blur, distortion and visual effects that can applied to a single object or area.
It's not a full-screen post processing effect, so it's faster to render as it only affects specific objects.

To use MystifyFX, select the desired object and add the MystifyFX component to it.

You can also right click in the hierarchy and select GameObject -> Effects -> Mystify FX.



Help & Support Forum
--------------------

Check the Documentation folder for detailed instructions.

Have any question or issue?
* Support-Web: https://kronnect.com/docs/mystify-fx/
* Support-Discord: https://discord.gg/EH2GMaM
* Email: contact@kronnect.com
* Twitter: @Kronnect

If you like Mystify FX, please rate it on the Asset Store. It encourages us to keep improving it! Thanks!
https://assetstore.unity.com/packages/package/303296#reviews




Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Mystify FX will be eventually available on the Asset Store.



More Cool Assets!
-----------------
Check out our other assets here:
https://assetstore.unity.com/publishers/15018



Version history
---------------

Version 3.0.4:
- [Fix] Rain effect now falls downwards on meshes with rotated or flipped UV islands

Version 3.0.3:
- Added support for Unity 6.7

Version 3.0.2:
- [Fix] Reset button inspector fix

Version 3.0:
- Added support for Unity 6.4

Version 2.2:
- Added Chromatic Aberration effect
- Scan lines: added rotation parameter

Version 2.1.1:
- Added support for "Compatibility Mode" in Render Graph

Version 2.1:
- Added "Texture Source" option: screen or object

Version 2.0:
- Added "profile" / "sharedProfile" behaviour to avoid modifying scriptable objects at runtime. Using profile will automatically instantiate the assigned profile.

Version 1.5:
- Added "Alpha Cutoff" parameter

Version 1.4:
- Include: added option to specify renderers by layer mask
- Rain effect improvements

Version 1.3:
- Include: added option to specify custom renderers

Version 1.2: 29/Dec/2024
- Added Hexagrid custom scale parameter

Version 1.1.2: 29/Dec/2024
- [Fix] Fixed a visibility issue with internal quad mesh

Version 1.1.1: 27/Dec/2024
- [Fix] Fixed an issue when viewing certain effects from a skewed angle

Version 1.1: 24/Dec/2024
- Added distortion rim power option
- Removed vertex program variants

Version 1.0: Dec/2024
- Initial release
