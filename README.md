This repository contains my mods for VRChat.

## AdvancedSafety
Features:
 * Set hard limits on avatar features, such as polygon count, audio sources, and some other things
   * Reduce crashes
   * Improve performance
   * Remove annoying spawn sounds or global sounds
   * Remove some fullscreen effects (this one is unreliable)
   * Avatars over the limit are not replaced by a gray robot. Instead, elements over limits are removed, with the rest of the avatar kept intact.
 * Hide all avatars of a specific author (requires UIExpansionKit, button is in user details menu)
 * Hide a specific avatar, no matter who uses it (requires UIExpansionKit, button in in user quick menu)
 * Hide portals from blocked users or non-friends
   * That blocked asshole will no longer be able to portal drop you

This mod will introduce small lag spikes when avatars are loaded. These should be tolerable compared to VRChat's own lag spikes.  
All numeric limits are configurable.  
 * Don't set animators limit to 0 - you will break all humanoid avatars horribly if you do  

Configurable for friends and vanilla "show avatar" button.  

## CameraMinus
Allows resizing the camera, zooming it in/out and hiding camera lens.  
This is a lazy rewrite of original [VRCCameraPlus](https://github.com/Slaynash/VRCCameraPlus) by Slaynash.  
Requires UIExpansionKit - new buttons are added to Camera QuickMenu expansion.  

## EmojiPageButtons
This mod adds page buttons to emoji menu that allow faster switching.  
Requires UIExpansionKit.  
![emoji page buttons screenshot](https://imgur.com/gIq2vKw.png)

## Finitizer
This mod fixes a set of issues arising from invalid floating point values being accepted from remote users.  
**It might have a minor impact on performance that scales with player and pickup count**. Only use this mod if you frequent publics.

## JoinNotifier
A VRChat mod to notify you when someone joins the instance you're in

Current features:
 - Visual and audible notifications (configurable)
 - Toggleable per instance type (public/friends/private)
 - Can be set to highlight friends or show only friends
 
## LocalPlayerPrefs
This mod moves game settings storage from Windows registry to UserData folder.  
This can make using multiple accounts easier by having separate installs for them.  
Do note that some settings will stay in registry (the ones that Unity itself uses as opposed to game code).  
There's also no import from registry, so expect to have to log in again after installing this mod. 
 
## MirrorResolutionUnlimiter
Headset and display resolutions increase each year, and yet VRChat limits mirror resolution to 2048 pixels per eye. With this mod, that's not the case anymore!  
Set whatever limit you want, with an option to un-potatoify mirrors that world makers set to potato resolution for their insane reasons. Or you can make all mirrors blurry as a sacrifice to performance gods. It's up to you, really.

Note that increasing mirror texture resolution will increase VRAM usage and lower performance, as your GPU will have to do more work.

If UIExpansionKit is installed, Settings page in the main menu will get two buttons to optimize and beautify all visible mirrors in the world.

Settings:
 * Max mirror resolution - the maximum size of eye texture for mirror reflections. 2048 is VRChat default, 4096 is mod default.
 * Force auto resolution - removes mirror resolution limits set by world maker. Off by default.
 * Mirror MSAA - changes MSAA specifically for mirrors. Valid values are 0 (same as main camera), 1, 2, 4 and 8. Lower MSAA may lead to "shimmering" and jaggies, especially in VR. 

## NoSteamAtAll
Makes the game unable to access Steam. At all.    
This prevents it from getting your SteamID, which means that it won't get sent to everyone in the instance. No more assholes taking a peek at your Steam profile!    
**This will also make you unable to log in via Steam.** Additionally, you may experience different voice quality. Nothing too bad though, it would be the same as what Oculus Store users get.

## ParticleAndBoneLimiterSettings
This mod provides an UI for changing VRChat's built-in dynamic bone and particle limiter settings.  
Refer to VRChat docs [for particle limiter](https://docs.vrchat.com/docs/avatar-particle-system-limits#particle-limiter-configuration-description) and [for dynamic bone limiter](https://docs.vrchat.com/docs/avatar-dynamic-bone-limits) for a detailed description of what these settings do.  
Changing these settings should not require game restart.  
Requires UIExpansionKit. Settings are placed into the Mod Settings menu.

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
 
 ## SparkleBeGone
 This mod allows removing start and end sparkles from VR laser pointers, as well as recoloring them.  
 It will do nothing on desktop.   
 Settings are fairly self-explanatory.
 
 ## UIExpansionKit
 This mod provides additional UI panels for use by other mods, and a unified mod settings UI.  
 Some settings (currently boolean ones) can be pinned to quick menu for faster access.  
 Refer to [API](UIExpansionKit/API) for mod integration.  
 MirrorResolutionUnlimiter has an [example](MirrorResolutionUnlimiter/MirrorResolutionUnlimiterMod.cs) of soft dependency on this mod  
 EmojiPageButtons has an [example](EmojiPageButtons/EmojiPageButtonsMod.cs) for delaying button creation until your mod is done
 
 This mod uses [Google Noto](https://www.google.com/get/noto/) font, licensed under [SIL Open Font License 1.1](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL).  

 
 ## HWIDPatch
This mod allows you to fake your Hardware ID. This mod creates a new ID on launch and saves it for future launches. The ID can be changed in `modprefs.ini` afterwards. Set it to empty string to generate a new one.  
Privacy first!

 
 ## CoreLimiter
A mod to automatically limit your game to a certain amount of CPU cores. This can be used to boost performance on some Ryzen CPUs by limiting the game to a single CCX.
Naturally, limiting the game may reduce maximum possible performance under heavy load, and results are highly dependent on how well the game is multithreaded.
  
You should experiment with settings in a CPU-heavy world or scene to measure performance on your specific system. For CPUs with less than 8 cores it might be worth it to reduce used core count or allow hyperthreads.  
  
This mod is Windows-only. It likely won't do anything on Intel CPUs, but you're free to experiment with it.    

Settings:
 * Max Cores (default 4) - the maximum amount of cores that the game may use. 4 is the sweet spot on a 2700X/3700X.
 * Skip Hyperthreads (default true) - don't assign game to both threads of one core. Works best when enabled on 2700X/3700X.

## Installation
Before install:  
**Tupper (from VRChat Team) said that any modification of the game can lead to a ban, as with these mods**

To install these mods, you will need to install [MelonLoader](https://discord.gg/2Wn3N2P) (discord link, see \#how-to-install).  
Then, you will have to put mod .dll files in the `Mods` folder of your game directory

## Building
To build these, drop required libraries (found in `<vrchat instanll dir>/MelonLoader/Managed` after melonloader installation, list found in `Directory.Build.props`) into Libs folder, then use your IDE of choice to build.
 * Libs folder is intended for newest libraries (MelonLoader 0.2.2)
