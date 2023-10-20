# ULTRAKILL: Vine Boom Mod

WARNING: This creation nor it's creator are affiliated with New Blood or Arsi "Hakita" Patala.

**ULTRAKILL: Vine Boom Mod** adds the vine boom sound whenever an enemy dies.

## Installing

1. [Install BepInEx, Version 5](https://docs.bepinex.dev/v5.4.21/articles/user_guide/installation/index.html).
2. Extract contents of the downloaded zip into ULTRAKILL.exe's path.
   1. Open Steam.
   2. Go to your library.
      - <img src="images/steam%20library.png">
   3. Right-click on ULTRAKILL, then left-click on "Properties..."
      - <img src="images/ultrakill%20properties.png">
   4. Place the `vineboom.zip` archive's contents into the game's folder.
3. [Configure the mod (Optional)](#Configuration)
4. Launch the game and enjoy! :3

## Configuring

Upon launching the game the mod will generate a configuration file in `BepInEx/Config/vul.somedevfox.vineboom.cfg` that can be edited using a notepad.
It will look something like this:

```ini
## Settings file was created by plugin *vine boom* v0.1.0
## Plugin GUID: vul.somedevfox.vineboom

[Enemy]
## How much should the sound increase in volume for all enemy types
# Setting type: Single
# Default value: 0.1
Global = 0.1

[Sound]
## Whether or not should the sound effect get progressively louder with each enemy kill
# Setting type: Boolean
# Default value: true
ProgressivelyGetsLouder = true

## What the maximum volume should be (1.0 is 100%)
# Setting type: Single
# Default value: 1
MaximumVolume = 1

## What the sound volume should be (this setting is not used if `ProgressivelyGetsLouder` setting is true)
# Setting type: Single
# Default value: 1
Volume = 10

## What sound effect should be used
# Setting type: String
# Default value: {{ModFolder}}/funny.mp3
FilePath = {{ModFolder}}/funny.mp3

[Timer]
## Whether the decay timer is enabled
# Setting type: Boolean
# Default value: true
Enabled = true

## When should the sound volume be reset in milliseconds
# Setting type: Int32
# Default value: 300000
DecayIn = 300000
```

To change a setting simply edit the value after the equals sign (`=`).

## Credits

- [Noboru "somedevfox"](https://github.com/somedevfox) - developer of this mistake.
- [zimberzimber](https://github.com/zimberzimber) - helped with coding <3
- [Carrak](https://github.com/Carrak) - helped with understanding Unity scene stuff.
  
