# Obentunes
This mod overhauls the radio object to allow a much wider selection of music choices.

## Installation
You'll need BepInEx 5 installed. \
Simply extract the Obentunes directory to `BepInEx/plugins`.

## Features
### Current:
- 50+ new radio tracks
  - This is not exhaustive compared to all the different sources of music heard in-game, so some things may be missing.
- Support for custom audio
- Radio GUI for track selection
- Volume controls
- Per-session radio persistence.
  - **NOTE**: This means radios will not remember what they were playing after a restart!
  - **NOTE 2**: Also, radio's don't respect the concept of timelines (seriously). If you play a track on a radio in one save, then load an older save that has the same radio, it will be playing there too! This can't be fixed unless I integrate with save files.

### Planned for the future:
- Pitch correction for specific tracks
- Save file integration (Kind of unlikely I'm scared to mess with anybody's save files)

## Custom Audio
Custom tracks are loaded from `Obentunes/custom`. Only wav and ogg are supported.

## Track Names
You can change the names of the default tracks by editing `Obentunes/res/TrackNames.json`.
The names I've chosen are loosely tied to where you hear each track in game. Otherwise they don't have any official names to my knowledge.
