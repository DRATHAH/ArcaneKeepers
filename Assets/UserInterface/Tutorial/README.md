# Tutorial (Dog_Hero guide)

First-level tutorial. Dog_Hero stands in the bottom-right corner and walks the player through the level with speech bubbles, a magic-wand pointer, glowing highlights and small reactions. It plays once (saved in PlayerPrefs).

## Folders
- `Scripts/Core`: `TutorialDirector` (runs the steps), `TutorialLines` (all dialogue, editable in the Inspector), `TutorialTheme` (art, colour and sound references), `TutorialKit` (helpers).
- `Scripts/Guide`: `DogGuide` (Dog_Hero: pop-in, idle, talking bob, reactions, emotes) and `SpeechBubble` (typewriter text, tap-to-continue arrow).
- `Scripts/Pointer`: `TutorialPointer` (the wand: point + tap, drag demo with a ghost card).
- `Scripts/Highlight`: `GlowHighlight` (UI outline glow), `TutorialSpotlight` (soft dim with a hole), `TileHighlight` (glow on a lane tile in the world), `LaneDemo` (arrows + hopping skulls that show the objective).
- `Scripts/Fx`: `TutorialFx` (sparkles, ripples, dust, confetti).
- `Scripts/Hud`: `WaveProgressMeter` (a PvZ-style wave bar at the top of the screen; works in any level).
- `Art`: generated sprites (`Tools/GenerateTutorialArt.py` makes them; `Art/Dog/DogGuide.png` is cut from `ArtSource/Dog-Hero.webp` (repo root)). The wand (crystal branch) and wave meter (wooden plank, fill, skull, flag) are hand-drawn style from `Tools/handdrawn_art.py` (needs numpy, Pillow, scipy); its `FILL_X` must match `WaveProgressMeter.fillMin/fillMax`.
- `Data/TutorialTheme.asset`: the theme. Drop AudioClips into its Sound slots for pop, talk-blip and cheer sounds.
- `Prefabs`: `Tutorial.prefab` and `WaveMeter.prefab` (both are in DemoLevel).
- `Editor/TutorialSetup.cs`: menu **Arcane Keepers > Tutorial**.

## Common tasks
- Play it again: **Arcane Keepers > Tutorial > Reset Tutorial Progress**.
- Change what Dog_Hero says: select `Tutorial` in DemoLevel, then edit **Lines**. `<k>word</k>` colours a word and `{name}` is replaced with the Keeper's name.
- Re-import the art and rebuild the theme and prefabs: **Arcane Keepers > Tutorial > Set Up Everything** (safe to run more than once).
- Add the wave meter to another level: drag `Prefabs/WaveMeter.prefab` into the scene.

## Steps
Welcome, objective (lanes + castle), wave meter, Keeper cards, **drag the first Keeper onto the glowing tile** (the player has to do it), card rest/recharge, **a second Keeper**, **move a Keeper** (tap the move button, then drag), then "Here they come!". The waves are held until the tutorial ends or the player taps SKIP. During that first battle Dog_Hero pops up for the first kill, a fallen Keeper and the final wave.
