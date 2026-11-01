# UserInterface

All UI lives here, one folder per screen. Each folder holds that screen's `Scripts`, `Art`, `Prefabs` and `Editor` (builder) code.

| Folder | Screen / scene |
|---|---|
| `Shared` | Used by several screens: `Fonts` (Lilita One + licence), `Style` (generated CC_/CS_ sprites, TMP font asset + materials), `Art` (Green background, hero portraits), `Scripts` (SceneTransition, CloudWipe, SafeArea, ScaleToFit, team info, `Audio`, `Fx` base kit + sky/motes), `Editor/MenuBuilder.cs` |
| `MainMenu` | MainMenu scene: logo, hero cluster, skeletons, logo + menu effects |
| `CharacterSelect` | TowerSelect scene: lineup, squad, tower info/upgrade bar, SelectCard prefab, its MenuBuilder parts |
| `LevelSelect` | LevelSelect scene: level tiles, unlock progress, effects |
| `Loading` | Loading scene: cat animation frames + LoadingScreen |
| `LevelHUD` | In-level HUD: tower cards + health bar, tower placing/relocating, Tile prefab |
| `PauseMenu` | In-level pause prefab (see its README) |
| `LevelEnd` | Win / lose screen. The prefab stays in `LevelEnd/Resources` because it is loaded with `Resources.Load` |
| `Tutorial` | First-level tutorial (see its README) |

The original art the cutouts are made from (webp, gif, checkerboard PNGs) is in `ArtSource/` at the repo root, outside Unity.
Move assets inside Unity (or with their `.meta` files) so links don't break.
