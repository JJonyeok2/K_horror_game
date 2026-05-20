# K Horror Unity Migration Project

This is the Unity-side migration workspace for the Godot prototype. The original
Godot `.gd` and `.tscn` files remain reference-only.

## Open

Open this folder in Unity Hub or with Unity Editor `6000.3.15f1`:

```text
Unity_Migration/KHorrorUnity
```

Unity has been opened once on this machine with Unity Personal activated. Unity
will regenerate ignored local folders such as `Library/`, `Logs/`, `Temp/`, and
`UserSettings/` when the project opens on another machine.

## Source Layout

- `Assets/KHorrorGame/Migration/CoreLoop/Scripts`: migrated C# gameplay loop, interaction, inventory, UI, and player controller.
- `Assets/KHorrorGame/Migration/CoreLoop/Tests`: EditMode state-machine tests.
- `Assets/KHorrorGame/Rendering/Scripts`: PBR material builder and lighting profile/rig.
- `Assets/KHorrorGame/Editor`: bootstrap tools for generating the first Unity scene.
- `Assets/External/ambientcg`: local copy target for ambientCG PBR textures.

## Local Asset Sync

The downloaded ambientCG files already exist in the repository under:

```text
assets/external/ambientcg/materials
```

They are not duplicated in git under the Unity project. To copy them locally for
Unity import, run one of:

```powershell
pwsh ./Tools/SyncAmbientCgAssets.ps1
```

```bash
bash ./Tools/sync_ambientcg_assets.sh
```

Then in Unity run:

```text
Tools/K Horror Migration/Build ambientCG Materials
```

## First Scene

After the project opens and packages restore, run:

```text
Tools/K Horror Migration/Create Bootstrap Scene
```

This generates `Assets/Scenes/KHorror_Main.unity` with the core roots, player,
lighting rig, bongo terminal proxy, estate proxy, extraction zone, artifact,
and settlement station wired to the migrated state machine.

## Command Line Tests

Run EditMode tests without `-quit`; Unity exits automatically after the test run:

```powershell
& "$env:LOCALAPPDATA\Unity\Editors\6000.3.15f1\Editor\Unity.exe" -runTests -batchmode -projectPath . -testPlatform EditMode -testResults .\TestResults.xml
```
