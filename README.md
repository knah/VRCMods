This repository contains my mods for VRChat. Join the [VRChat Modding Group discord](https://discord.gg/rCqKSvR) for support and more mods!

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

## FavCat
An all-in-one local favorites mod. Unlimited favorite lists with unlimited favorites in them and a searchable local database of content and players.  
**Requires UIExpansionKit 0.2.0 or newer**  
#### Features:
* Unlimited lists (categories) for favorites, each of unlimited size
* Lag-free even with large lists
* Freely changeable list height
* Avatar, world, and player favorites supported
* Modifiable list order and multiple list sorting options
* Fully searchable database of everything you have ever seen
* Changeable database location (**it's recommended to store the database in a directory backed up to cloud storage, such as Dropbox or OneDrive**, see below for setup)
* Local image cache for even better performance
* Categorize your own private avatars
* Import avatar favorites from other local favorite mods (read below)
* Exchange search database with friends (read below)
* Many more small things

#### Known limitations
* Player favorites don't show online status
* Lists with over a thousand elements can take a bit of time on game startup/list creation

#### Changing database location
Steps to change database location:
1. Run VRChat with the mod at least once
2. Make sure that VRChat is closed
3. Navigate to VRChat install directory (i.e. by clicking "Browse Local Files" in Steam)
4. Navigate to `UserData` folder and open `MelonPreferenes.cfg` with Notepad or other text editor
5. Find the line with `[FavCat]`
6. Find the line with `DatabasePath` under it
7. Change the value to absolute path to new storage folder. The new line should look like this: `DatabasePath = "C:/Users/username/OneDrive"` (with your own path, naturally; make sure to use forward clashes `/` instead of backslashes `\\`)
8. Save and close the text file
9. Copy the two (or four) database files (`favcat-favs.db` and `favacat-store.db`, and `favcat-favs-log.db` and `favcat-store-log.db` if they exist) from the old location (they are in `UserData` by default) to the new one.

If you want to move the image cache, use the same steps as above, but modify the line with `ImageCachePath` and copy `favcat-images.db` instead. It's not recommended to store the image cache in cloud storage due to its big size.

#### Importing avatar favorites from other local favorite mods
You can import favorites from other local favorite mods that use text files for storage.
1. Run VRChat with the mod at least once
2. Navigate to VRChat install directory (i.e. by clicking "Browse Local Files" in Steam)
3. Find the avatar list of the mod you want to import from. Places to look like are `UserData` folder, game folder, or a mod-specific folder, such as `404Mods`. The avatar list would usually be a plain text file or a JSON file - both are supported.
4. **Copy** the file to `UserData/FavCatImport` folder
5. In-game, click "More FavCat" on any big menu page, then click "Import databases and text files"
6. Import process can take some time. Once it is done, a new list will appear in the menu. It's safe to close the game and reopen it - import will continue from where it left off (you'll need to click the button again though). Once it is done, the corresponding list will be deleted from `UserData/FavCatImport` folder.

#### Sharing search database with friends
You can exchange the search database with friends to be able to find things they have seen. **Only accept databases from friends you trust - an intentionally malformed database can overwrite parts of yours with garbage**  
How to send database to a friend:
1. Run VRChat with the mod at least once (duh)
2. Make sure that VRChat is closed
3. Navigate to where your database is stored (see "Changing database location")
4. Make sure that there is no file named `favcat-store-log.db`. If there is one, it means that the game was not closed properly. In that case, run the game again, and use "Exit VRChat" button in settings menu to close it.
5. Send `favcat-store.db` to your friend.

How to receive database from a friend:
1. Run VRChat with the mod at least once
2. Navigate to VRChat install directory (i.e. by clicking "Browse Local Files" in Steam)
3. Put the database your friend sent you into `UserData/FavCatImport` folder. If you want to import multiple databases at once, you can rename them, as long as .db extension is kept.
4. In-game, click "More FavCat" on any big menu page, then click "Import databases and text files"
5. Import process can take some time. Once it is done, the corresponding database will be deleted from `UserData/FavCatImport` folder.

Note that your favorites are stored in `favcat-favs.db` - don't send it to your friends, favorite import is not supported. Most certainly don't send `favcat-images.db` to your friends - it's just a boring image cache.

#### Used libraries:
* [LiteDB](https://github.com/mbdavid/LiteDB) for all data storage
* [ImageSharp](https://github.com/SixLabors/ImageSharp), because unity is bad at loading images from streams on background thread

A long time ago this was based on Slaynash's [AvatarFav](https://github.com/Slaynash/AvatarFav) and [VRCTools](https://github.com/Slaynash/VRCTools), both licensed under the [MIT license](https://github.com/Slaynash/VRCTools/blob/master/LICENSE). Who knows how much of that still remains inside?


## Finitizer
This mod fixes a set of issues arising from invalid floating point values being accepted from remote users.  
**It might have a minor impact on performance that scales with player and pickup count**. Only use this mod if you frequent publics.

## Friends+ Home
Allows changing instance type of your home world to whatever you want.  
Setting it to public will choose a random populated public instance if one is available.  
UIExpansionKit 0.2.0 or newer recommended for in-game settings.  
Known instance types are `Public`, `FriendOfGuests`, `FriendsOnly`, `InvitePlus` and `InviteOnly` (if you wish to edit modprefs.ini by hand)

## IKTweaks
This mod offers a customized VRIK solver for full body tracking, and a few other IK-related tweaks.  
Features:
* No more viewpoint drift in FBT. Instead, your spine bends (up to a limit), or your hip drifts (above the limit).
* No more weird chest rotations when laying down or upside down
* No more weird spine/neck stretching
* Remote players see the new IK too, there's no mismatch between what you and others see
* Support for universal calibration - calibrate once for all avatars, even ones using different rigs or proportions
* Support for per-avatar calibration saving (when not using universal calibration)
* Half-click head follow calibration: hold one trigger to freeze the avatar in place to be able to look at your feet
* Support for elbow, knee and chest trackers (read below)
* Optional local NetIK pass to ensure you see the same thing as remote players (not necessary for Index Controller users)
* Disable FBT even if you have trackers connected, for when you're charging them from your PC

It's recommended to use a normal humanoid rig without any rig hacks (so no neck fix, no FBT fix, no inverted hip, no zero-length spine bones).  
It requires at least three trackers (legs and hip). 3-point (no trackers) and 4-point (hip tracker) modes are not affected by the mod.

### Using additional trackers
You need to enable additional trackers in mod settings before you're able to use them.  
To use knee trackers, there are no additional requirements - just calibrate normally.  
To use elbow or chest trackers, you'll need to stand straight and T-pose your arms during calibration.  
Chest tracker is kinda useless and janky, so don't bother buying a tracker for it.  
It's recommended to put elbow/knee trackers as close to the joint they're tracking as possible (but not on the joint itself). For arms, the recommended position is on the outer surface of the lower or upper arm next to the elbow.  
If you're using additional trackers, your avatar should generally match your physical proportions - that is, all body parts should line up reasonably well without real height hacks.

### Partial source code
This mod includes parts of FinalIK, which is a paid Unity Store asset, therefore source code for those is not provided.  
If you want to build the mod yourself, you'll need to do the following:
* Get a copy of FinalIK from asset store
* Copy the VRIK solver, VRIK component and TwistRelaxer component into mod sources folder
* Rename them to match with what the rest of mod source expects, make VRIK_New `partial`
* Add the following line to start of `RootMotionNew.FinalIK.IKSolverVR.Spine.FABRIKPass` : `weight = Mathf.Clamp01(weight - pelvisPositionWeight);`
* Remove `RootMotionNew.FinalIK.IKSolverVR.Spine.SolvePelvis` from the original VRIK solver
* Rename `RootMotionNew.FinalIK.IKSolverVR.Leg.ApplyOffsets` to `ApplyOffsetsOld`, remove `override` from it
* Add `ApplyBendGoal();` to the second line of `RootMotionNew.FinalIK.IKSolverVR.Leg.Solve(bool)`
* Rename `Update`, `FixedUpdate` and `LateUpdate` on VRIK_New by adding `_ManualDrive` suffix to them and make them `internal` instead of `private`
* Fix compilation if broken

## JoinNotifier
A VRChat mod to notify you when someone joins the instance you're in

Current features:
 - Visual and audible notifications (configurable)
 - Toggleable per instance type (public/friends/private)
 - Can be set to highlight friends or show only friends
 - Custom join/leave sounds - put files named `JN-Join.ogg` and/or `JN-Leave.ogg` into `UserData` folder to override default sounds (they must be in Ogg Vorbis format, naturally)

## Lag Free Screenshots
This mod significantly improves screenshot taking performance for handheld camera in VR and F12 key in desktop mode. Benefits are especially noticeable with higher resolution screenshots (4K/8K).  
Additionally you can set your screenshots to be saved as JPEG files instead of PNG to save on file size. JPEG encoding takes a bit of time though, but it doesn't freeze your game anyway so why even care?
 
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

## True Shader Anticrash
This mod prevents practically all known shader crashes. Note that it can affect how stuff looks as it rewrites shader code to be non-crashy. Setting changes require world rejoin to reload shaders.
### Partial source code
Main logic of this mod is located in the native DLL that currently is not opensource. The DLL is build upon [HLSLcc](https://github.com/Unity-Technologies/HLSLcc) and uses [Microsoft Detours](https://github.com/microsoft/Detours). An opensource release for it will likely be available at a later point.

 
 ## UIExpansionKit
 This mod provides additional UI panels for use by other mods, and a unified mod settings UI.  
 Some settings (currently boolean ones) can be pinned to quick menu for faster access.  
 Refer to [API](UIExpansionKit/API) for mod integration.  
 MirrorResolutionUnlimiter has an [example](MirrorResolutionUnlimiter/MirrorResolutionUnlimiterMod.cs) of soft dependency on this mod  
 EmojiPageButtons has an [example](EmojiPageButtons/EmojiPageButtonsMod.cs) for delaying button creation until your mod is done
 
 This mod uses [Google Noto](https://www.google.com/get/noto/) font, licensed under [SIL Open Font License 1.1](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL).  

## View Point Tweaker
This mod allows you to move view point ("view ball") on avatars. The tweak will affect only you, but other players will see your adjusted head position correctly.  
Adjusted view points are saved per avatar.  
**Requires UI Expansion Kit 0.2.0+**. The menu to tweak view point can be found in UI Elements Quick Menu submenu.  
Do note that the coordinates displayed in that menu are local offset of the view point, not the coordinates you set in avatar descriptor.
 
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

## ILRepack
There's a copy of [ILRepack.Lib.MSBuild.Task](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task) and [ILRepack](https://github.com/gluck/il-repack) built for netcore/MSBuild 16 shipped with the repo.

## Installation
Before install:  
**Tupper (from VRChat Team) said that any modification of the game can lead to a ban, as with these mods**

To install these mods, you will need to install [MelonLoader](https://discord.gg/2Wn3N2P) (discord link, see \#how-to-install).  
Then, you will have to put mod .dll files in the `Mods` folder of your game directory

## Building
To build these, drop required libraries (found in `<vrchat instanll dir>/MelonLoader/Managed` after melonloader installation, list found in `Directory.Build.props`) into Libs folder, then use your IDE of choice to build.
 * Libs folder is intended for newest libraries (MelonLoader 0.2.2)

## License
With the following exceptions, all mods here are provided under the terms of [GNU GPLv3 license](LICENSE)
* ILRepack.Lib.MSBuild.Task is covered by [its own license](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task/blob/master/LICENSE.md)
* ILRepack is covered by [Apache 2.0 license](https://github.com/gluck/il-repack/blob/master/LICENSE)
* UI Expansion Kit is additionally covered by [LGPLv3](UIExpansionKit/COPYING.LESSER) to allow other mods to link to it
* IKTweaks source code is not covered by a permissive license and provided for reference purposes only