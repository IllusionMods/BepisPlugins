# BepisPlugins
A collection of essential BepInEx plugins for Koikatu / Koikatsu Party, EmotionCreators, AI-Shoujo / AI-Girl, HoneySelect2, HoneyCome, SamabakeScramble / Summer Vacation Scramble, Aicomi, AmanatsuLocation and other games by Illusion/Illgames. Check plugin descriptions below for a full list of included plugins. 

## How to install
1. Install the latest version of [BepInEx](https://github.com/BepInEx/BepInEx). Make sure it is installed and working before installing BepisPlugins.
   - For HoneySelect2 and games older than it, get BepInEx 5.
   - For RoomGirl/HoneyCome and games newer than it, get BepInEx 6 (nightly build 668 or later).
2. Install the latest version of the [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) plugin.
3. Download the latest release archive for your game (specified by the two letter prefix, e.g. AI for AI-Girl) from the releases page (not the "Clone or download" button).
4. Extract the archive into your game directory (where the game exe and BepInEx folder are located). Replace old files if asked.

If you need help, check the #help channel on the [Koikatsu discord server](https://discord.gg/hevygx6).

You can get the latest nightly builds of all plugins from the [CI workflow](https://github.com/IllusionMods/BepisPlugins/actions/workflows/ci.yaml). Open the latest successful run and download the build from the Artifacts section.

## If you are a modder

If you'd like to create a mod that is compatible with various plugins in this repo, scroll down.

If you'd like to contribute code fixes and improvements: fork this repository, create a new branch, push your changes, and open a new PR.

To build this repository you will need VisualStudio2022+ with the `.NET desktop development` and `Game development for Unity` workloads, and `.NET Framework 3.5 development tools` + targetting packs and SDKs for at least `.NET Framework 4.6` (best to just install them all).
All dependencies are downloaded via nuget on first build of the solution. Check the wiki if you are having issues with build steps failing.
To make a release, remove old `bin\out` folder and rebuild the whole repo in Release configuration. Afterwards run release.ps1 to generate the combined zip files.

You can discuss modding on the [Koikatsu discord server](https://discord.gg/hevygx6) in the modding channels. There are also various modding guides linked in the pins of these channels you may want to check out.

## Plugin descriptions
You can see more information about some of the plugins by checking their config files in `BepInEx\config` (or by using the in-game [ConfigurationManager plugin](https://github.com/BepInEx/BepInEx.ConfigurationManager)).

Note: Not all plugins might be available for a given game (not yet ported by anyone, or technically infeasible).

### BGMLoader
Loads custom BGMs and clips played on game startup. Stock audio is replaced during runtime by custom clips from BepInEx\BGM and BepInEx\IntroClips directories.

[Tutorial on how to replace sound clips and background music using BGMLoader.](https://github.com/IllusionMods/BepisPlugins/wiki/BGM-Loader)

### ColorCorrector
Allows configuration of some post-processing filters. (change of bloom amount, disable saturation filter)

### ExtensibleSaveFormat
Allows additional data to be saved to character, coordinate and scene cards. The cards are fully compatible with non-modded game, the additional data is lost in that case. This is used by sideloader to store used mod information.

### InputUnlocker
Allows user to input longer than normal values to InputFields. This allows longer names and other properties stored as text.

### Screencap / Screenshot Manager
Creates screenshots based on settings. Can create screenshots of much higher resolution than what the game is running at. It can make screen (F9 key) or character (F11 key) screenshots.

Screencap has a public API that can be used by other plugins to create screenshots or adjust the game screen as it is being captured (e.g. to disable effects that do not get captured correctly).
Check the "Public API" region near the top of [ScreenshotManager.cs](.\src\Core_Screencap\ScreenshotManager.cs).

### Sideloader
Loads mods packaged in .zip archives from the Mods directory without modifying the game files at all. You don't unzip them, just drag and drop to Mods folder in the game root.

It prevents mods from colliding with each other thanks to the UniversalAutoResolver subsystem (i.e. 2 mods have same item IDs and can't coexist; Sideloader automatically assigns correct IDs). It also makes it easy to disable/remove mods with no lasting effects on your game install (just remove the .zip, no game files are changed at any point).

> Note: Sideloader is not available for games by Illgames because of technical reasons (IL2CPP). You will have to use [SardineTail](https://github.com/MaybeSamigroup/SVS-SardineTail/wiki) for them instead.

[More information and tutorial on sideloader-compatible mod creation.](https://github.com/IllusionMods/BepisPlugins/wiki/1-Introduction-to-zipmod-format)

[Step-by-step guide for creating a simple texture mod.](https://github.com/IllusionMods/BepisPlugins/wiki/2-How-to-create-a-simple-zipmod)

[Tool for automatically converting old list mods to sideloader-compatible form.](https://github.com/IllusionMods/ZipStudio/releases)

### SliderUnlocker
Allows user to set values outside of the standard 0-100 range on all sliders in the editor.

### IMGUIModule.Il2Cpp.CoreCLR.Patcher
Fixes issues preventing IMGUI plugin windows (e.g. ConfigurationManager) from working in some games that use IL2CPP.
This is caused by imperfect IL2CPP support in BepInEx 6. It is not needed for games that use Mono (HoneySelect2, Koikatu, etc.).

## Removed plugins

### Configuration Manager
Moved to https://github.com/BepInEx/BepInEx.ConfigurationManager

### DeveloperConsole
Moved to https://github.com/BepInEx/DeveloperConsole

### IPALoader
Moved to https://github.com/BepInEx/IPALoaderX

### MessageCenter
Moved to https://github.com/BepInEx/MessageCenter

### ScriptEngine
Moved to https://github.com/BepInEx/BepInEx.Debug

## Obsolete plugins
### DynamicTranslationLoader
Replaced by [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator)

### ResourceRedirector
Replaced by [XUnity.ResourceRedirector](https://github.com/bbepis/XUnity.AutoTranslator)
