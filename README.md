
# BepisPlugins
A collection of essential [BepInEx](https://github.com/BepInEx/BepInEx) plugins for Koikatu / Koikatsu Party, EmotionCreators, AI-Shoujo / AI-Girl, HoneySelect2 and other games by Illusion. Check plugin descriptions below for a full list of included plugins. 

If you wish to contribute or need help, check the #help channel on the [Koikatsu discord server](https://discord.gg/hevygx6).

[![GitHub release](https://img.shields.io/github/release/bbepis/BepisPlugins.svg?style=for-the-badge)](https://github.com/bbepis/BepisPlugins/releases)
[![Github Releases](https://img.shields.io/github/downloads/bbepis/BepisPlugins/latest/total.svg?style=for-the-badge)](https://github.com/bbepis/BepisPlugins/releases)

### How to install
0. At least [BepInEx 5.0](https://builds.bepis.io/projects/bepinex_be) is required. Make sure it is installed and working before installing BepisPlugins.
1. Download the latest release archive for your game (specified by the two letter prefix, e.g. AI for AI-Girl) from the releases page above (not the "Clone or download" button).
2. Extract the archive into your game directory (where the game exe and BepInEx folder are located). Replace old files if asked.

## Plugin descriptions
You can see more information about some of the plugins by checking their config files in `BepInEx\config` (or by using the in-game [ConfigurationManager plugin](https://github.com/BepInEx/BepInEx.ConfigurationManager)).

Note: Not all plugins might be available for a given game (not yet ported by anyone, or technically infeasible).

### BGMLoader
Loads custom BGMs and clips played on game startup. Stock audio is replaced during runtime by custom clips from BepInEx\BGM and BepInEx\IntroClips directories.

[Tutorial on how to replace sound clips and background music using BGMLoader.](https://github.com/IllusionMods/BepisPlugins/wiki/BGM-Loader)

### ColorCorrector
(Koikatsu)

Allows configuration of some post-processing filters. (change of bloom amount, disable saturation filter)

### ExtensibleSaveFormat
Allows additional data to be saved to character, coordinate and scene cards. The cards are fully compatible with non-modded game, the additional data is lost in that case. This is used by sideloader to store used mod information.

### InputUnlocker
Allows user to input longer than normal values to InputFields. This allows longer names and other properties stored as text.

### Screencap
Creates screenshots based on settings. Can create screenshots of much higher resolution than what the game is running at. It can make screen (F9 key) or character (F11 key) screenshots.

### Sideloader
Loads mods packaged in .zip archives from the Mods directory without modifying the game files at all. You don't unzip them, just drag and drop to Mods folder in the game root.

It prevents mods from colliding with each other (i.e. 2 mods have same item IDs and can't coexist; sideloader automatically assigns correct IDs). It also makes it easy to disable/remove mods with no lasting effects on your game install (just remove the .zip, no game files are changed at any point).

[More information and tutorial on sideloader-compatible mod creation.](https://github.com/IllusionMods/BepisPlugins/wiki/1-Introduction-to-zipmod-format)

[Step-by-step guide for creating a simple texture mod.](https://github.com/IllusionMods/BepisPlugins/wiki/2-How-to-create-a-simple-zipmod)

[Tool for automatically converting old list mods to sideloader-compatible form.](https://github.com/IllusionMods/ZipStudio/releases)

### SliderUnlocker
Allows user to set values outside of the standard 0-100 range on all sliders in the editor.

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
