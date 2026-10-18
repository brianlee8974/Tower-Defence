# Python Tower Defence Unity port

These scripts port the original game's core play loop. The art and audio files are copied into `Assets/TowerDefence/Art` and `Assets/TowerDefence/Audio`.

## Scene setup

1. Create an orthographic 2D camera with **Size = 3**. The original map is 1000 x 600 pixels; this uses 100 source pixels per Unity unit. Place the background sprite centered at `(0, 0, 0)` and set its import Pixels Per Unit to 100.
2. Create an empty `Route` object and add 11 child transforms in order. Convert each original point `(x, y)` from `Settings.py` with `X = x / 100 - 5`, `Y = 3 - y / 100`. Assign those transforms to `WaveManager.waypoints` and put `spawnPoint` at the first waypoint.
3. Add an `Enemy` component to an enemy prefab with a `SpriteRenderer`; optionally add a world-space Canvas with a Slider and assign it as `healthBar`.
4. Create three tower prefabs with `Tower`, `SpriteRenderer`, and `Collider2D`. Set their kind, range (Archer 2.0, Cannon 1.5, Magician 1.8), starting damage (10, 10, 5), and purchase cost (75, 100, 125). Cannon uses its splash radius field for area damage.
5. Place `TowerSpot` objects at the 12 original `corTower` locations, each with a `Collider2D`. Add `TowerPlacement` to `Route` or another active manager object and assign the three prefabs and camera. Tower prefabs also need a `Collider2D` so they can be selected. Assign a build panel containing the three tower buttons and a separate tower-actions panel containing Upgrade/Sell to the matching `TowerPlacement` fields. The panels start hidden: clicking an empty spot opens the build panel; clicking a built tower opens the actions panel; clicking elsewhere closes both.
6. Add `GameManager` to an active scene object. Add `WaveManager` to `Route` and assign the enemy prefab, route, and enemy sprites in its three `enemyTypes` entries (triangle, square, hexagon). Assign optional HUD labels using legacy `UnityEngine.UI.Text`. Use one play/pause button: put a `Button` and `Image` on it, assign the Image and the `play.png`/`pause.png` sprites to WaveManager, then connect its OnClick to `WaveManager.OnWaveButtonPressed`. It displays Pause while a wave is running and Play while paused or ready for a wave; clicking it pauses/resumes an active wave or starts the next one. Connect speed to `GameManager.ToggleDoubleSpeed`; connect tower buttons to `TowerPlacement.BuildArcher/BuildCannon/BuildMagician`, and tower actions to `UpgradeSelected`/`SellSelected`.
7. For each UI Button, set **Transition** to **Color Tint** and choose a brighter **Highlighted Color** for hover feedback. You can also use **Sprite Swap** if you create separate hover sprites. In the EventSystem, use `InputSystemUIInputModule` (the project already includes Unity Input System and UGUI). Add an AudioSource for music/effects as desired; the files are imported but not yet wired into playback.
8. For menu screens, add a `MenuController` and assign the main, difficulty, settings, help, about, and gameplay panels. Connect menu buttons to `ShowDifficulty`, `ShowSettings`, `ShowHelp`, `ShowAbout`, and `ShowMain`; connect difficulty buttons to `StartEasy/StartNormal/StartHard`, and the quit button to `QuitGame`. Connect a volume Slider if desired. Keep `GameManager` available when `Start*` is pressed (the simplest setup uses all panels in one scene).

The game starts paused with 300 money and 5 lives, like the Python version. Press the play/next-wave button to begin. Completing a wave pauses the game until the next wave is started. Difficulty's damage multiplier is set on GameManager: Easy `.5`, Normal `.7`, Hard `1` (matching the values used in the Python code). Tower upgrades affect all existing towers of that type and also apply to towers built later.

## Tower animation setup

Tower attacks can animate directly from the existing sprite files without an Animator Controller. On each tower prefab's `Tower` component, assign its `SpriteRenderer`, leave `Idle Sprite` as the standing sprite (or set it explicitly), and fill `Attack Frames` with the five attack sprites in order. Archer uses `Aarcher1`–`Aarcher5`, Cannon uses `Acannon1`–`Acannon5`, and Magician uses `Amagician1`–`Amagician5`. The `Tower` script plays those frames when it fires and returns to the idle sprite. `Attack Frame Rate` defaults to 10 FPS. An Animator Controller with an `Attack` Trigger is also supported when no attack sprite frames are assigned.

## Port notes

- The original 15 wave compositions are in `WaveManager`. Enemy movement speed converts the original pixels-per-frame at 30 FPS into Unity units-per-second.
- This is a code and asset port, not a ready-made scene or prefab set. The Unity scene needs the references and UI connections described above.
- Original upgrade damage increments and prices are preserved. Tower firing cadence and cannon splash radius are exposed in the Inspector so they can be tuned against the original animation/audio timing.
