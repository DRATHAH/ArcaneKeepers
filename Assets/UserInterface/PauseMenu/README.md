# Pause Menu

Everything for the in-level pause screen lives in this folder.

| Folder | What's in it |
| --- | --- |
| `Scripts/PauseMenu.cs` | Pause / Continue / Restart / Give Up logic (freezes `Time.timeScale`). |
| `Scripts/PauseMenuAnimator.cs` | All the motion: open, close, button squash/bounce, audio icon wiggle. |
| `Prefabs/PauseCanvas.prefab` | The whole screen. Every level uses an instance of this prefab. |
| `Art/` | Generated button icons (pause, play, restart, flag). |
| `Art/Original/` | Earlier hand-made pause art. Not used any more, kept for reference. |
| `Editor/MenuBuilder.PauseMenu.cs` | Builds the prefab and puts it in the levels. |

## Look
Uses the shared Clash of Critters style: stone card, blue ribbon banner, chunky buttons
(blue = Continue, gold = Restart, red = Give Up), round blue music/sound toggles, Lilita One font.
The shared sprites (`CC_*`, `CS_Rune`) are in `Assets/UserInterface/Shared/Style`.

## Adding it to a new level
Drag `Prefabs/PauseCanvas.prefab` into the scene (the scene needs an EventSystem).
`Assets/Prefabs/LevelTemplate.prefab` already has it.

## Rebuilding
**Arcane Keepers > Rebuild Pause Menu** regenerates the icons and the prefab, then replaces the
pause menu in DemoLevel, Level2, Level3, EnemyTester and the LevelTemplate prefab.
Add new level scenes to `PauseScenes` in the builder.
