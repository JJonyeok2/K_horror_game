# K Horror Unity Migration Project

This is the Unity-side migration workspace for the Godot prototype. The original
Godot `.gd` and `.tscn` files remain reference-only.

## Open

Open this folder in Unity Hub or with Unity Editor `6000.3.15f1`:

```text
Unity_Migration/KHorrorUnity
```

Unity batchmode project creation was blocked on this machine because the Editor
license has not been activated yet. After Unity Hub signs in and activates a
Personal/Pro license, Unity will regenerate `Library/`, package lock files, and
missing generated settings.

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
