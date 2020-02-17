This repository contains my mods for VRChat.


## JoinNotifier
A VRChat mod to notify you when someone joins the instance you're in

Current features:
 - Visual and audible notifications (configurable)
 - Toggleable per instance type (public/friends/private)

## RuntimeGraphicsSettings
A mod to allow tweaking some graphics settings at runtime to get those extra few frames.
If only VRCTools supported editing integer values at runtime...

Settings description:
 * -1 on integer settings means "don't change the default value"
 * MSAALevel - multi-sampled anti-aliasing level. Valid values are 2, 4 and 8
 * AllowMSAA - toggle MSAA at runtime
 * AnisotropicFiltering - texture anisotropic filtering
 * RealtimeShadows - allow realtime shadows
 * SoftShadows - use soft shadows if shadows are enabled. Soft shadows are more expensive.
 * PixelLights - maximum amount of pixel lights that can affect an object
 * Texture decimation - Reduces texture resolution by 2^(this setting). A value of 0 means full-resolution textures, a value of 1 means half-res, 2 would be quarter res, and so on.
 * GraphicsTier - Unity Graphics Hardware Tier. Valid values are 1, 2 and 3. Only affects shaders loaded after it was changed. Probably of questionable value in VRChat, as custom shaders rarely support this setting.

## VRCTools and AvatarFav forks (as submodules)
These forks have VRCModNetwork entirely removed and store avatar data locally. For those of you who are extra paranoid or malicious.
Some mods that rely on VRCModNetwork could break if you use these. These will also break if you use them together with normal ones.
Avatar search still works, based on locally observed avatars.
There's no way to import your VRCModNetwork/AvatarFav favorites list.

## Installation
Before install:  
**Tupper (from VRChat Team) said that any modification of the game can lead to a ban, as with these mods**

To install these mods, you will need to install [VRCModLoader](https://github.com/Slaynash/VRCModLoader).  
Then, you will have to put mod .dll files in the `Mods` folder of your game directory

## Building
To build these, drop required libraries (found in `<vrchat instanll dir>/VRChat/VRChat_Data/Managed` and mods folder, list found in `Directory.Build.props`) into Libs folder, then use your IDE of choice to build.
