This repository contains my mods for VRChat. Join the [VRChat Modding Group discord](https://discord.gg/rCqKSvR) for support and more mods!  
Looking for more (universal) mods? [Check out my universal mods repository!](https://github.com/knah/ML-UniversalMods)

## Preface
Modifying the VRChat client is not allowed by VRChat Terms of Service and can lead to your account being banned.  
Based on evidence until this point, VRChat Team will happily hand out bans to people using mods with evil features, such as ripping, crashing, and others things you don't want to be on the receiving end of. 
Mods published here aim to avoid negative attention from VRChat Team and hopefully keep their users in a non-banned state as long as possible.  
However, with that said, **these mods are provided without any warranty and as-is**. 
If you use them and do get banned, don't complain to me that you were not warned. 
Also, report your ban to VRChat Modding Group discord (see above) - every data point matters in determining how safe mods are for general public use.

As an extra point, if you encounter an issue with the game (especially after an update), **make sure that issue is not caused by mods before reporting it to VRChat Team**.  
You can add `--no-mods` launch option (in Steam) to temporarily disable MelonLoader without uninstalling it completely.

Some mods will have Canny tickets linked. It's your duty to go and upvote them to show to VRChat Team what you want from their game. If you decide to comment on Canny, **be respectful** and **avoid mentioning modding** - the team still doesn't like it, obviously.  
And yes, it will probably be ignored/forever hanging in "under review" like the majority of Canny posts. At least we'll have a nice big upvote number on our ignored posts.

## Installation
To install these mods, you will need to install [MelonLoader](https://discord.gg/2Wn3N2P) (discord link, see \#how-to-install).
Then, you will have to put mod .dll files in the `Mods` folder of your game directory. Dll files for mods are provided under the [releases page](https://github.com/knah/VRCMods/releases).

## AdvancedSafety
Features:
 * Set hard limits on avatar features, such as polygon count, audio sources, and some other things
   * Reduce crashes
   * Improve performance
   * Remove annoying spawn sounds or global sounds
   * Remove some fullscreen effects (this one is unreliable)
   * Avatars over the limit are not replaced by a gray robot. Instead, elements over limits are removed, with the rest of the avatar kept intact.
 * Hide all avatars of a specific author (requires UI Expansion Kit, button is in user details menu)
 * Hide a specific avatar, no matter who uses it (requires UI Expansion Kit, button in in user quick menu)
 * Hide portals from blocked users or non-friends
   * That blocked user will no longer be able to portal drop you

This mod will introduce small lag spikes when avatars are loaded. These should be tolerable compared to VRChat's own lag spikes.  
All numeric limits are configurable.  
 * Don't set animators limit to 0 - you will break all humanoid avatars horribly if you do  

Configurable for friends and vanilla "show avatar" button.  

**Canny tickets**:
* [Allow changing safety settings based on trust rank of avatar uploader, not wearer](https://feedback.vrchat.com/feature-requests/p/allow-changing-safety-settings-based-on-trust-rank-of-avatar-uploader-not-wearer) (I wanted to implement this, but it would be too API request heavy to be safe)
* [Allow hiding all avatars made by a specific user](https://feedback.vrchat.com/feature-requests/p/allow-hiding-all-avatars-made-by-a-specific-user)
* [Hiding specific avatars](https://vrchat.canny.io/feature-requests/p/blocking-specific-avatars)

## CameraMinus
Allows resizing the camera, zooming it in/out and hiding camera lens.  
This is a lazy rewrite of original [VRCCameraPlus](https://github.com/Slaynash/VRCCameraPlus) by Slaynash.  
Requires UI Expansion Kit - new buttons are added to Camera expando (collapsed by default, click the small blue square near camera to expand) or to Camera QuickMenu expansion (can be chosen in mod settings).  

**Canny tickets**:
* [A few camera suggestions](https://feedback.vrchat.com/feature-requests/p/a-few-camera-suggestions)
* [Move the camera lens to not be instrusively in the middle of the camera screen](https://feedback.vrchat.com/feature-requests/p/move-the-camera-lens-to-not-be-instrusively-in-the-middle-of-the-camera-screen)

## EmojiPageButtons
This mod adds page buttons to old emoji menu that allow faster switching.  
Requires UI Expansion Kit.  
![emoji page buttons screenshot](https://imgur.com/gIq2vKw.png)

## FavCat
An all-in-one local favorites mod. Unlimited favorite lists with unlimited favorites in them and a searchable local database of content and players.  
**Requires UI Expansion Kit 0.2.0 or newer**  
#### Features:
* Unlimited lists (categories) for favorites, each of unlimited size
* Lag-free even with large lists
* Freely changeable list height
* ~~Avatar,~~ world, and player favorites supported
* Modifiable list order and multiple list sorting options
* Fully searchable database of everything you have ever seen
* Changeable database location (**it's recommended to store the database in a directory backed up to cloud storage, such as Dropbox or OneDrive**, see below for setup)
* Local image cache for even better performance
* ~~Categorize your own private avatars~~
* ~~Import avatar favorites from other local favorite mods (read below)~~
* Exchange search database with friends (read below)
* Hide default lists that you never use  
* Many more small things

#### Known limitations
* Player favorites don't show online status
* Lists with over a thousand elements can take a bit of time on game startup/list creation

#### Canny tickets
* [**Reconsider the approach to paywalling extra avatar favorite slots/groups**](https://feedback.vrchat.com/vrchat-plus-feedback/p/reconsider-the-approach-to-paywalling-extra-avatar-favorite-slotsgroups)
* [The ability to categorize avatars](https://feedback.vrchat.com/feature-requests/p/the-ability-to-categorize-avatars)
* [Search avatars](https://feedback.vrchat.com/feature-requests/p/search-avatars)
* [Personal avatar sorting](https://feedback.vrchat.com/feature-requests/p/personal-avatar-sorting)

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

#### Avatar favorites deprecation
Due to recent events surrounding modding (modder ban-unban wave, VRC Team/modders discussion, API changes), I've decided to remove extra avatar favorites from FavCat.  
One of the biggest concerns raised during the still-ongoing discussion between modders and VRChat Team is mods stepping on VRC+ features. VRChat relies on VRC+ supporters to pay for servers and ensure that VRChat continues existing.  
As such, I wanted to proactively address that concern. One of my main goals with modding VRChat is making the game better for everyone, and negatively affecting the platform itself goes against that.    
I believe this step to be necessary to ensure that VRChat team sees (wholesome) modding not as a threat, but as an opportunity.  
On top of that, recent API changes indicate that VRChat Team is taking action to restrict access to avatars. It might be an attempt at ripping prevention, or it might be aimed at privacy enhancement, or it might be aimed at extended avatar favorites. There's no way to tell for sure, and there's no knowing how far those changes would go in the future. 
Local favorite mods have to rely on the API to some degree, and with things changing quickly, there's no reliable way to ensure that things will stay working and stay safe ban-wise in the long run.

Before you read on, please [scroll up a bit](https://github.com/knah/VRCMods#canny-tickets) and **upvote all linked Canny tickets related to avatar favorites**. If you decide to comment on them, remember to **stay civil** and **avoid mentioning mods**.

In more practical terms, this means the following:
 * Starting with this update, you will not be able to add new avatar favorites, create new avatar favorite lists, or import avatar favorite lists  
 * Starting on 2021-05-31, you will not be able to access your existing avatar favorite lists in-game
 * Avatar search will stay accessible for the time being.
 * World and user favorites will stay accessible for the time being.  
   VRChat Team did mention during the recent dev stream that they are going to provide additional player and world favorites for everyone at some later point.
 * You can export all your favorites into plain text files from "More FavCat..." menu in the respective menu page. Exported lists are put in `UserData/FavCatExport` folder.
   Export will be always available and is not subject to time limits mentioned above.  
   This has limited usability, but you probably can put them on pedestals in your own world or find an udon world that has change-pedestal-to-id function (unless that gets restricted eventually).   
   This is still somewhat iffy, but it's unwieldy/uncomfortable enough compared to in-menu favorite lists that I wouldn't consider that as a viable replacement for VRC+ extra favorites.

##### FAQ:
**Q1**: But what about *another mod X* that still offers unlimited avatar favorites for everyone?  
**A1**: Good question! If that is a VRCMG mod, it'll likely follow suit.   
If it's not a VRCMG mod, if I were you, I'd be *extremely* wary of that. Those mods are unverified in VRCMG, and usually for a good reason.  
There have been numerous cases of unverified mods being harmful towards their own users. After all, if one makes a mod to harm other users and/or VRChat itself, who says that they wouldn't want to harm its own users eventually for some petty reason?  
Additionally, consider this: recently there was a relatively big ban wave against users of a certain malicious/unverified mod. It's not too hard to imagine that another malicious/unverified mod will be next.

**Q2**: Does that mean that mods are now okay if they don't touch VRC+?  
**A2**: Nope! According to VRChat Team all mods are still against Terms of Service and therefore bannable, nothing has changed there.

**Q3**: Why only remove avatar favorites, but not world and/or player favorites?  
**A3**: Simply because extra world/player favorites are not a VRC+ feature.  
If, for whatever reason, VRChat decides to remove extra avatar favorites from VRC+ (for example by giving 100 slots to everyone), modded extra favorites will likely come back.

**Q4**: Why remove avatar favorites completely instead of restricting them to VRC+ users?  
**A4**: A fully local mod can't be reliably restricted.
Significant codebase drift, however, is at least mildly annoying to deal with, which reduces probability of questionable forks continuing to provide the same functionality. 

**Q5**: Someone is/will be distributing a modified version of this mod with this dumb restriction removed.  
**A5**: See question one. Also, that's not my problem anymore. Also, if it's **you** who happens to provide that version, make sure your version is easily distinguishable from the mainline one, for example by adding a suffix to the name or version number.

**Q6**: People who don't want to pay for VRC+ won't pay for it anyway, why even bother? / **Answer 2,** therefore why even bother?  
**A6**: It's not only about VRC+, but also about sending a message. There's much to be gained from cooperating with VRChat Team. Monetization/cash flow is an important concern for any company. Being pointlessly contrarian here doesn't help anyone. 

**Q7**: This is a blatant VRC+ cash grab!  
**A7**: If you choose to look at it that way. VRChat needs to get money from somewhere to pay (likely huge) server costs, and eventually repay investors on top of that. Blatantly and purposefully denying them that is simply an asshole thing to do. Also see question 9.    

**Q8**: Today they come for our extra favorites, tomorrow they'll come for *modded feature Y*!  
**A8**: Perhaps. I can't affect VRChat Team's decisions to paywall certain features.  
However, based on the dev stream, it seems unlikely that we'll see much paywalling - there was no mention of "VRC+ only" in respect to any of the new features, except for the few features *specifically for* other VRC+ features.  

**Q9**: You're a sellout/shill! / MelonLoader devs are sellouts! / *Other personal attack or insult*! / *Other rumor without any proof*!  
**A9**: I get it, you're 14 and angry. And I have better things to do than needlessly fighting VRChat Team about the direction they want to take their game in and getting a bunch of people banned along the way.
 
**Closing word**:  
With the introduction of VRC+, users and their feedback became much more important, simply because VRChat depends more than ever on user support to continue existing and developing.  
You now have the option of voting with your wallet (purchasing/cancelling VRC+ depending on your opinion of the direction VRChat is going in), in addition to talking to your friends about it, providing feedback to VRChat via discord or Canny, and exploring competing social VR platforms.  
Capitalism at its best and all that.

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
UI Expansion Kit 0.2.0 or newer recommended for in-game settings.  
Known instance types are `Public`, `FriendOfGuests`, `FriendsOnly`, `InvitePlus` and `InviteOnly` (if you wish to edit modprefs.ini by hand)

**Canny tickets**:
* [Allow setting your home world to a friends/friends+ instance](https://feedback.vrchat.com/feature-requests/p/allow-setting-your-home-world-to-a-friendsfriends-instance)

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

**Canny tickets**:
* [More trackers for fullbody tracking](https://feedback.vrchat.com/feature-requests/p/more-trackers-for-fullbody-tracking)
* [Add control to temporarily disable FBT](https://feedback.vrchat.com/feature-requests/p/add-control-to-temporarily-disable-fbt)
* [Retain FBT Calibration settings during play session](https://feedback.vrchat.com/feature-requests/p/retain-fbt-calibration-settings-during-play-session)
* [Full Body Tracking problems](https://feedback.vrchat.com/feature-requests/p/full-body-tracking-problems)
* [Avatar local neck stretching on some setups](https://feedback.vrchat.com/bug-reports/p/avatar-local-neck-stretching-on-some-setups)
* [Shoulder move is different when using only VR and fullbody tracking](https://feedback.vrchat.com/bug-reports/p/shoulder-move-is-different-when-using-only-vr-and-fullbody-tracking)
* [Spine always straight with full body tracking](https://feedback.vrchat.com/bug-reports/p/spine-always-straight-with-full-body-tracking)
* [Full body spine/chest/neck stretching occurs locally with specific controllers](https://feedback.vrchat.com/bug-reports/p/full-body-spinechestneck-stretching-occurs-locally-with-specific-controllers)
* Check comments on some of those - they have links to other related posts

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

**Canny tickets**:
* [Join / Leave notifications](https://feedback.vrchat.com/feature-requests/p/join-leave-notifications)

## Lag Free Screenshots
This mod significantly improves screenshot taking performance for handheld camera in VR and F12 key in desktop mode. Benefits are especially noticeable with higher resolution screenshots (4K/8K).
Additional features:
 * You can set your screenshots to be saved as JPEG files instead of PNG to save on file size.  
 * Automatically rotate screenshots so that proper side faces up (like on your real phone!)
 * Add metadata about world and players to screenshot files (disabled by default; both JPEG and PNG are supported, though PNG metadata is not displayed by Windows - you'll have to use a use different photo viewer software)

**Canny tickets**:
* [Record or correct camera orientation](https://feedback.vrchat.com/feature-requests/p/record-or-correct-camera-orientation)
* [A few camera suggestions](https://feedback.vrchat.com/feature-requests/p/a-few-camera-suggestions)
* [8K camera](https://feedback.vrchat.com/feature-requests/p/8k-camera)

## MirrorResolutionUnlimiter
Headset and display resolutions increase each year, and yet VRChat limits mirror resolution to 2048 pixels per eye. With this mod, that's not the case anymore!  
Set whatever limit you want, with an option to un-potatoify mirrors that world makers set to potato resolution for their insane reasons. Or you can make all mirrors blurry as a sacrifice to performance gods. It's up to you, really.

Note that increasing mirror texture resolution will increase VRAM usage and lower performance, as your GPU will have to do more work.

If UI Expansion Kit is installed, Settings page in the main menu will get two buttons to optimize and beautify all visible mirrors in the world.

Settings:
 * Max mirror resolution - the maximum size of eye texture for mirror reflections. 2048 is VRChat default, 4096 is mod default.
 * Force auto resolution - removes mirror resolution limits set by world maker. Off by default.
 * Mirror MSAA - changes MSAA specifically for mirrors. Valid values are 0 (same as main camera), 1, 2, 4 and 8. Lower MSAA may lead to "shimmering" and jaggies, especially in VR.

**Canny tickets**:
* [problem of maximum resolution of mirror](https://feedback.vrchat.com/feature-requests/p/problem-of-maximum-resolution-of-mirror)

## ParticleAndBoneLimiterSettings
This mod provides an UI for changing VRChat's built-in dynamic bone and particle limiter settings.  
Refer to VRChat docs [for particle limiter](https://docs.vrchat.com/docs/avatar-particle-system-limits#particle-limiter-configuration-description) and [for dynamic bone limiter](https://docs.vrchat.com/docs/avatar-dynamic-bone-limits) for a detailed description of what these settings do.  
Changing these settings should not require game restart.  
Requires UI Expansion Kit. Settings are placed into the Mod Settings menu.

**Canny tickets**:
* [Particle Limiter](https://feedback.vrchat.com/feature-requests/p/particle-limitier)

## SparkleBeGone
This mod allows removing start and end sparkles from VR laser pointers, as well as recoloring them.  
It will do nothing on desktop.   
Settings are fairly self-explanatory.

## True Shader Anticrash
This mod prevents practically all known shader crashes. Note that it can affect how stuff looks as it rewrites shader code to be non-crashy. Setting changes require world rejoin to reload shaders.
### Partial source code
Main logic of this mod is located in the native DLL that currently is not opensource. The DLL is build upon [HLSLcc](https://github.com/Unity-Technologies/HLSLcc) and uses [Microsoft Detours](https://github.com/microsoft/Detours). An opensource release for it will likely be available at a later point.

 
## UI Expansion Kit
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

## ILRepack
There's a copy of [ILRepack.Lib.MSBuild.Task](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task) and [ILRepack](https://github.com/gluck/il-repack) built for netcore/MSBuild 16 shipped with the repo.

## Building
To build these, drop required libraries (found in `<vrchat instanll dir>/MelonLoader/Managed` after melonloader installation, list found in `Directory.Build.props`) into Libs folder, then use your IDE of choice to build.
 * Libs folder is intended for newest libraries (MelonLoader 0.3.0)

## License
With the following exceptions, all mods here are provided under the terms of [GNU GPLv3 license](LICENSE)
* ILRepack.Lib.MSBuild.Task is covered by [its own license](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task/blob/master/LICENSE.md)
* ILRepack is covered by [Apache 2.0 license](https://github.com/gluck/il-repack/blob/master/LICENSE)
* UI Expansion Kit is additionally covered by [LGPLv3](UIExpansionKit/COPYING.LESSER) to allow other mods to link to it
* IKTweaks source code is not covered by a permissive license and provided for reference purposes only
