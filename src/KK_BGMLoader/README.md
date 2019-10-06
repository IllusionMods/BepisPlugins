## BGMLoader
Loads custom BGMs and clips played on game startup. Stock audio is replaced during runtime by custom clips from BepInEx\BGM and BepInEx\IntroClips directories.

### Intro clips
Intro clips replace the "Koikatsu!" voice clips when the game starts. They have to be placed inside BepInEx\IntroClips and be in .wav format. They can have any filenames, as long as they have the .wav extension, e.g. `\Koikatsu\BepInEx\IntroClips\evil-laugh.wav`

### BGM clips
BGM clips replace the background music played in the game. They have to be placed inside BepInEx\BGM and be in .ogg format. Their filenames need to be in BGMxx.ogg format, where xx is the number of the background music, e.g. `\Koikatsu\BepInEx\BGM\BGM00.ogg`

#### List of background clips used by the game
```
BGM00 - Title
BGM01 - MapMoveDay
BGM02 - MapMoveEve
BGM03 - Custom
BGM04 - Communication
BGM05 - Encounter
BGM06 - Lover
BGM07 - Anger
BGM08 - HSceneGentle
BGM09 - HScene
BGM10 - HScenePeep
BGM11 - Sad
BGM12 - Reminiscence
BGM13 - Date
BGM14 - MorningMenu
BGM15 - NightMenu
BGM16 - SystemMenu
BGM17 - StaffRoom
BGM18 - Daytime
BGM19 - Evening
BGM20 - Memories
BGM21 - StaffRoom_01
BGM22 - NightMenu_01
BGM23 - Cool
BGM24 - Lively
BGM25 - Park
BGM26 - Caffe
BGM27 - Config
```