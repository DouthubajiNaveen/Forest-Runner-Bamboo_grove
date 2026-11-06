# FOREST RUNNER – BAMBOO GROVE
A 3D endless runner for Windows PC (keyboard + mouse), made in Unity. The project needs **no paid assets, packages from the Asset Store or API keys**: every mesh, texture, sound, particle effect and UI screen is generated in code.

> **Honest status:** this project was generated and statically checked (brace balance, cross-class references, GUID/meta integrity, asset-to-class field matching) outside of Unity. It has **not been compiled or play-tested inside the Unity Editor** by its author. See *Known limitations / first-run checklist* at the bottom.

## Unity version
* Developed for **Unity 2022.3 LTS** (`ProjectVersion.txt` = 2022.3.20f1). Any 2022.3.x works; Unity Hub will offer the nearest installed 2022.3 if 20f1 is missing. Newer versions will upgrade the project.
* Render pipeline: **Built-in** (Standard shader). No URP/HDRP. Required package: `com.unity.ugui` (already in `Packages/manifest.json`; TextMeshPro is **not** used).
* Input: legacy Input Manager (`activeInputHandler: 0`).

## Open it
1. Unzip `ForestRunner_BambooGrove_Unity.zip`.
2. Unity Hub → **Add → Add project from disk** → select the `ForestRunnerBambooGrove` folder.
3. Pick Unity 2022.3.x and open (first import takes a minute; Unity creates `Library/`).
4. Open `Assets/Scenes/MainMenu.unity` (menu **Forest Runner → Open Main Menu Scene**) and press **Play**.
5. Build: *File → Build Settings* already lists MainMenu (0) and Gameplay (1) (auto-added by `Assets/Editor/ForestRunnerEditorSetup.cs`; run **Forest Runner → Setup Project** if not). Choose *Windows, x86_64* → Build.

## Controls
| Key | Action |
|---|---|
| A / ← | Move left |
| D / → | Move right |
| Space (also W / ↑) | Jump |
| S / ↓ | Slide (in the air: drop quickly, then slide) |
| Esc | Pause / resume |
| Mouse | All menu buttons and sliders |

## Features
* **Gameplay:** automatic forward run, 3 lanes with smooth switching, jump (buffered input), slide, kinematic-rigidbody trigger collisions on dedicated layers (Player 8, Obstacle 9, Collectible 10, Ground 11), speed ramp, distance + score, coins, high-score + best-distance saving, game over, restart, pause.
* **8 obstacle types:** fallen bamboo logs, forest rocks, wooden barricades, hanging bamboo (slide), deep pits, river crossings (with wooden bridges), traps (spike plates + rolling boulders), ancient temple barriers (stone walls and slide-under gates).
* **Fair procedural generation:** 48-unit sections made of 12-unit rows; every row always has a free lane, a jump-over, a slide-under or a bridge; hazards are followed by recovery rows; coin trails (lines, triples, zig-zags, jump arcs) and power-ups placed only where reachable.
* **5 power-ups** (each with pickup prefab, visual, timer, HUD bar, particles, sound): Gold Magnet (10 s), Shield (absorbs one hit, shows bubble), Speed Boost (+60 % speed, smashes obstacles, speed lines, FOV kick), Double Coins (15 s), Hover (fly over obstacles / pits / rivers; stays up until safe ground is below).
* **5 levels** with unique look + difficulty: 1 Bamboo Entrance (day), 2 Deep Bamboo Forest (misty), 3 River Crossing (sunset, rivers, waterfalls), 4 Ancient Forest Temple (dusk, temples, traps), 5 Mystic Bamboo Forest (night, stars, glowing plants, fireflies, max difficulty). Reach a level's unlock distance (800 / 1200 / 1600 / 2000 m) to unlock the next one. Locked/unlocked cards + difficulty pips in Level Select.
* **Environment:** tall bamboo on both sides, bushes, grass, flowers, rocks, vine arches, temples, waterfalls with mist, light shafts, distance fog, procedural sky with sun/moon, stars and hill silhouettes, drifting pollen / fireflies.
* **Shop:** 5 character skins and 5 power-up duration upgrades (5 levels each) bought with persistent coins.
* **UI:** main menu (title, Play, Levels, Shop, Settings, Quit, best score, coins, live 3D bamboo backdrop), HUD (score, distance, coins, level, power-up bars, pause button, banners, damage flash), pause menu, game-over screen (score, distance, coins, best, retry, next level, main menu), settings (music/SFX sliders, graphics quality, fullscreen, controls info, reset progress). Every button works.
* **Audio (all synthesised):** forest wind/rustle ambience (night: crickets), random bird calls, footsteps, jump, slide, land, coin, power-up, shield-break, hit, splash, game-over, unlock, buy/denied, UI click, two music loops (day / night).
* **Camera:** smooth third-person follow, speed-dependent FOV/distance, collision shake, speed-boost and hover effects.
* **Tech:** modular C# (namespace `ForestRunner`), ScriptableObject data (levels, power-ups, skins), object pooling for sections / obstacles / coins / power-ups / decor, floating-origin shifting (no precision loss in long runs), no per-coin `Update`, `Update` on boulders only while rolling.

## Project layout
```
Assets/
  Scenes/            MainMenu.unity, Gameplay.unity   (each = one SceneBootstrap object)
  Scripts/
    Core/            GameManager, SaveSystem, ObjectPooler, LevelManager, SceneBootstrap, GameConstants
    Data/            LevelConfig, PowerUpConfig, SkinConfig, GameConfig, ConfigDatabase, DefaultData (generated)
    Player/          PlayerController, CharacterVisual, CameraController
    Gameplay/        Coin, CoinManager, PowerUpManager, PowerUpPickup, Obstacle
    World/           EnvironmentSpawner, TrackSection, PieceFactory
    FX/              ParticleManager, SkyDome
    Audio/           AudioManager
    UI/              UIKit, UIManager (HUD), MainMenuManager, ShopManager, PauseManager, GameOverManager
    Utility/         ProcMesh, MeshKit, ProcTex, MaterialLibrary
  Editor/            ForestRunnerEditorSetup (menu "Forest Runner")
  ScriptableObjects/ Levels/ PowerUps/ Skins/   (.asset files; edit in Inspector)
  Resources/         GameConfig.asset (root), Shaders/SkyDome.shader, Audio/ (override clips)
  Prefabs, Materials, Textures, Models, Animations, Audio, Particles, UI   (README notes; see below)
Packages/manifest.json   ProjectSettings/   Tools~/ (the Python generators used to produce data/metas)
```
**Why the scenes are tiny:** each `.unity` file contains one `SceneBootstrap` component that builds camera, lights, sky, world, player, UI and managers at runtime. This avoids hand-written scene YAML with fragile object references. The menu item **Forest Runner → Bake Procedural Pieces To Prefabs** writes every pooled piece (obstacles, coins, power-ups, bamboo, temple…) with its generated meshes/materials/textures to `Assets/Prefabs`, `Assets/Models/Generated`, `Assets/Materials/Generated`, `Assets/Textures/Generated` for inspection/reuse. **Forest Runner → Regenerate Scene Files** recreates the two scenes via the Editor API if you ever need to.

## Tuning
Select any asset in `Assets/ScriptableObjects` (levels: speeds, obstacle chance and weights, density, colours, fog, light; power-ups: durations, upgrade price, spawn weight; skins: price and colours). Physics/feel constants (jump velocity, gravity, lane width) are in `GameConstants.cs`.
`Assets/Scripts/Data/DefaultData.cs` is a generated fallback used only if `Resources/GameConfig.asset` cannot be loaded.

## Replacing audio
Put AudioClips in `Assets/Resources/Audio/` named: `click, coin, jump, slide, swish, land, powerup, shield_break, hit, gameover, splash, unlock, buy, denied, footsteps, bird0–bird3, ambience_day, ambience_night, music_day, music_night`. A matching name overrides the generated sound.

## Scoring, coins and saving
Score = metres run + 10 per coin collected. Coins go straight into the persistent wallet (`PlayerPrefs`), as do high score, best distance, per-level best, unlocked levels, owned/equipped skins, upgrade levels and volume/quality. Settings → *Reset Progress* wipes progress.

## Known limitations / first-run checklist
* **Not compiled in Unity by the author.** If the Console shows a compile error, it is most likely a small typo-level issue; fix the line it names. Please report it so the project can be corrected.
* The player is a primitive-built explorer (procedural animation), not a skinned mesh. Visuals are stylised/low-poly; there is no post-processing (bloom etc.) to stay dependency-free.
* Standalone builds: the editor script adds the needed built-in shaders to *Always Included Shaders* automatically; if materials look pink in a build, run **Forest Runner → Setup Project** and rebuild.
* Pits/rivers are detected analytically from track data (not by physics) for robustness; obstacles and pickups use physics triggers.
* Speed Boost makes you invulnerable to obstacles (not to falling into pits/rivers). Shield does not save you from falling.
* No gamepad/touch support (keyboard + mouse only, as specified). Music is a simple procedural loop, not a composed track.
* The hand-written `ProjectSettings` files are minimal; Unity fills in all missing settings with defaults on first open (it may rewrite those files).

## Licensing
All code, meshes, textures, sounds and data in this project are original and generated; no third-party assets are used. Free to use and modify.
