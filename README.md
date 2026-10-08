# Obentunes
This mod overhauls the radio object to allow a much wider selection of music choices.

## Installation
You'll need BepInEx 5 installed. \
Simply extract the Obentunes directory to `BepInEx/plugins`.

## Features
### Current:
- 45+ new radio tracks
  - This is not exhaustive compared to all the different sources of music heard in-game, so some things may be missing.
- Radio GUI for track selection
- Per-session radio persistence.
  - **NOTE**: This means radios will not remember what they were playing after a restart!
  - **NOTE 2**: Also, radio's don't respect the concept of timelines (seriously). If you play a track on a radio in one save, then load an older save that has the same radio, it will be playing there too! This can't be fixed unless I integrate with save files.

### Planned for the future:
- Volume slider
- Customizable track names
- Support for custom music
- Save file integration (Kind of unlikely I'm scared to mess with anybody's save files)

## About Track Names
Most music in-game is not named by Loiste (to my knowledge). What I've done is figure where each track is most prominently heard and name them based off that. Sometimes this name is helpful and sometimes it's extremely generic. There are also some "Unknown" tracks where I genuinely couldn't pin a good name for them. 

Yes I am aware some of the music comes from Infra; since not everybody has played or is aware of Infra I opted to ignore it regarding those tracks.

If you have better names in mind, HMU.
