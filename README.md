This repository contains my mods for VRChat.


## JoinNotifier
A VRChat mod to notify you when someone joins the instance you're in

Current features:
 - Visual and audible notifications (configurable)
 - Toggleable per instance type (public/friends/private)
 
## MirrorResolutionUnlimiter
Headset and display resolutions increase each year, and yet VRChat limits mirror resolution to 2048 pixels per eye. With this mod, that's not the case anymore!  
Set whatever limit you want, with an option to un-potatoify mirrors that world makers set to potato resolution for their insane reasons. Or you can make all mirrors blurry as a sacrifice to performance gods. It's up to you, really.

Note that increasing mirror texture resolution will increase VRAM usage and lower performance, as your GPU will have to do more work.

Settings:
 * Max mirror resolution - the maximum size of eye texture for mirror reflections. 2048 is VRChat default, 4096 is mod default.
 * Force auto resolution - removes mirror resolution limits set by world maker. Off by default.

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
 
 ## CoreLimiter
A mod to automatically limit your game to a certain amount of CPU cores. This can be used to boost performance on some Ryzen CPUs by limiting the game to a single CCX.
Naturally, limiting the game may reduce maximum possible performance under heavy load, and results are highly dependent on how well the game is multithreaded.
  
You should experiment with settings in a CPU-heavy world or scene to measure performance on your specific system. For CPUs with less than 8 cores it might be worth it to reduce used core count or allow hyperthreads.  
  
This mod is Windows-only. It likely won't do anything on Intel CPUs, but you're free to experiment with it.    

Settings:
 * Max Cores (default 4) - the maximum amount of cores that the game may use. 4 is the sweet spot on a 2700X/3700X.
 * Skip Hyperthreads (default true) - don't assign game to both threads of one core. Works best when enabled on 2700X/3700X.

## AvatarFav fork (as submodule)
This fork has VRCModNetwork entirely removed and stores avatar data locally. For those of you who are extra paranoid or malicious.
Avatar search still works, based on locally observed avatars.
There's no way to import your VRCModNetwork/AvatarFav favorites list.

## Installation
Before install:  
**Tupper (from VRChat Team) said that any modification of the game can lead to a ban, as with these mods**

To install these mods, you will need to install [MelonLoader](https://discord.gg/2Wn3N2P) (discord link, see \#how-to-install).  
Then, you will have to put mod .dll files in the `Mods` folder of your game directory

## Building
To build these, drop required libraries (found in `<vrchat instanll dir>/MelonLoader/Managed` after melonloader installation, list found in `Directory.Build.props`) into Libs folder, then use your IDE of choice to build.
 * Libs folder is intended for newest libraries (MelonLoader 0.1.1/Unhollower 0.4.0)
 * LibsAlt folder is intended for compat libraries (MelonLoader 0.1.0/Unhollower 0.3.1)
