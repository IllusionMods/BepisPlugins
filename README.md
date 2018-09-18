# BepisPlugins
A collection of essential [BepInEx](https://github.com/BepInEx/BepInEx) plugins for Koikatu. Most importantly this collection includes a screenshot plugin, slider and input field unlockers, and a configuration manager that allows users to easily configure all plugins in a single place. This collection also includes multiple plugins useful for developers and power users.

[Frequently asked questions.](https://github.com/bbepis/BepisPlugins/wiki/FAQ)

## Releases
[![GitHub release](https://img.shields.io/github/release/bbepis/BepisPlugins.svg?style=for-the-badge)](https://github.com/bbepis/BepisPlugins/releases)
[![Github Releases](https://img.shields.io/github/downloads/bbepis/BepisPlugins/latest/total.svg?style=for-the-badge)](https://github.com/bbepis/BepisPlugins/releases)

These plugins are fairly tightly bound to BepInEx and often require a new version of the framework to work. If you experience problems update both [BepInEx](https://github.com/BepInEx/BepInEx/releases) and [BepisPlugins](https://github.com/bbepis/BepisPlugins/releases) to their latest stable versions.

You can download nightly builds [here](http://bepisbuilds.dyn.mk/bepis_plugins), but be prepared to face some bugs! If you find any problems update to the latest nightly for BepInEx and BepisPlugins. If the problem persists, please report it to the [Issues](https://github.com/bbepis/BepisPlugins/issues) page.

## Plugin descriptions
### BGMLoader
Loads custom BGMs and clips played on game startup. Stock audio is replaced during runtime by custom clips from BepInEx\BGM and BepInEx\IntroClips directories.

[Tutorial on how to replace sound clips and background music using BGMLoader.](BGMLoader/README.md)

### ColorCorrector
Allows configuration of some post-processing filters. (change of bloom amount, disable saturation filter)

### Configuration Manager
An easy way to let user configure how a plugin behaves without the need to make your own GUI. The user can change any of the settings you expose, even keyboard shortcuts. The configuration manager can be accessed from in-game settings screen.

Most of the plugins in this pack can be configured from this plugin. Some settings are hidden under the "Advanced settings".

[Tutorial on how to make your plugin compatible with Configuration Manager.](ConfigurationManager/README.md)

![Configuration manager](ConfigurationManager/Screenshot.PNG)

### DeveloperConsole
Show a console with real-time log from BepInEx and plugins. Console is opened and closed with F12 key. It's possible to dump scene contents to a text file from the console UI.

### DynamicTranslationLoader
Replaces text and image assets on runtime with files supplied in BepInEx\translation folder. No game files are modified in the process, everything is done in memory. This plugin can also be used to dump text and image assets to the disk for later translation.

### ExtensibleSaveFormat
Allows additional data to be saved to character, coordinate and scene cards. The cards are fully compatible with non-modded game, the additional data is lost in that case. This is used by sideloader to store used mod information.

### IPALoader
Loads IPA plugins from BepInEx\IPA folder. IPA should not be installed when this plugin is used. The loader has very good compatibility, but in rare cases some plugins might have issues.

### InputUnlocker
Allows user to input longer than normal values to InputFields. This allows longer names and other properties stored as text.

### MessageCenter
Displays pop-up text messages in the GUI whenever a plugin posts a Message event.

### ResourceRedirector
Allows other plugins to intercept and modify assets as they are loaded. This is notbly used by sideloader and translation loader for their core functionality.

### Screencap
Creates screenshots based on settings. Can create screenshots of much higher resolution than what the game is running at. It can make screen (F9 key) or character (F11 key) screenshots.

### ScriptEngine
Loads and reloads BepInEx plugins from the BepInEx\scripts folder. User can reload all of these plugins by pressing Ctrl+Delete. Useful for development.

### Sideloader
Loads mods packaged in .zip archives from the Mods directory without modifying the game files at all. You don't unzip them, just drag and drop to Mods folder in the game root.

It prevents mods from colliding with each other (i.e. 2 mods have same item IDs and can't coexist; sideloader automatically assigns correct IDs). It also makes it easy to disable/remove mods with no lasting effects on your game install (just remove the .zip, no game files are changed at any point).

[More information and tutorial on sideloader-compatible mod creation.](https://github.com/bbepis/BepisPlugins/wiki/Creating-.zip-mods)

[Tool for automatically converting old list mods to sideloader-compatible form.](https://mega.nz/#!lB8jQQab!tZ_yQy-F2Czig5JcRNFUxnSgJtFShck4kx3eEhm40HM)

### SliderUnlocker
Allows user to set values outside of the standard 0-100 range on all sliders in the editor.
