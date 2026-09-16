# Coin Rush Survivor

A mobile-first 2D survival prototype built with Unity. The project focuses on short arcade runs, escalating difficulty, collectible progression, run upgrades, persistent meta progression, cosmetics, revive hooks, and touch-friendly UI.

## Highlights

- Survival-run state machine with pause, revive, scoring, coins, XP, and difficulty scaling.
- Runtime progression with level-up choices and stackable run upgrades.
- Persistent player profile, permanent upgrades, cosmetics, and local save data.
- Enemy spawning, pooling, pickups, player health, and mobile/touch input.
- Menu, HUD, settings, pause, revive, run-end, and meta-progression presenters.
- Music/SFX service with saved volume and enable/disable preferences.
- Editor tooling that builds the vertical-slice scenes, prefabs, and ScriptableObjects automatically.
- Safe-area support for mobile UI.

## Tech

- Unity `2022.3.62f3` (LTS)
- C#
- Unity UI (`com.unity.ugui`)
- Built-in Audio and Physics 2D APIs

## Getting Started

1. Install Unity `2022.3.62f3` through Unity Hub.
2. Clone this repository and open the repository root as a Unity project.
3. Allow Unity to restore packages and import assets.
4. If the generated vertical slice is not created automatically, use:
   - `Coin Rush Survivor > Build Vertical Slice`
   - or `Coin Rush Survivor > Rebuild Vertical Slice`
5. Open `Assets/_Project/Scenes/Bootstrap.unity` and enter Play Mode.

The editor builder generates these scenes when needed:

- `Bootstrap.unity`
- `MainMenu.unity`
- `Game.unity`
- `MetaProgression.unity`

## Project Structure

```text
Assets/_Project/
├── Data/
├── Resources/
│   ├── Audio/
│   └── UI/Fonts/
├── Scripts/
│   ├── Core/
│   ├── Data/
│   ├── Editor/
│   ├── Enemies/
│   ├── Gameplay/
│   ├── Pickups/
│   ├── Player/
│   ├── Progression/
│   └── UI/
└── ...generated scenes, prefabs and ScriptableObjects
```

## Notes

Unity-generated `Library`, `Logs`, `UserSettings`, `Temp`, `Obj`, and build-output directories are intentionally excluded from version control. The project uses Unity's built-in Audio and Physics 2D APIs alongside Unity UI.

## Third-Party Assets

Third-party font and music attribution is kept in:

`Assets/_Project/Data/ThirdPartyNotices/`

Notably, the project includes Bungee and Nunito fonts under the SIL Open Font License 1.1 and two Kevin MacLeod music tracks under CC BY 4.0. Those assets remain under their respective licenses.

## License

The original project-specific source code and original project materials are copyright © 2026 Mohamed Anwar. All rights reserved unless explicitly stated otherwise. Third-party assets remain governed by their own licenses; see the notices above.
