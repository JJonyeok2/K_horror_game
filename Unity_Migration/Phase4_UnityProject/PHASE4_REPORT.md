# Phase 4 Unity Project Bootstrap Report

## Goal

Create an actual Unity project folder inside the repository so the migrated C#
code can be opened, restored, and assembled in Unity Editor `6000.3.15f1`.

## Output

Unity project root:

```text
Unity_Migration/KHorrorUnity
```

Created structure:

```text
KHorrorUnity
+- Assets
|  +- KHorrorGame
|  |  +- Migration/CoreLoop/Scripts
|  |  +- Migration/CoreLoop/Tests
|  |  +- Rendering/Scripts
|  |  +- Editor
|  +- External/ambientcg
|  +- Scenes
+- Packages
+- ProjectSettings
+- Tools
```

## Migrated Content

- Copied Phase 1 gameplay C# scripts into the Unity `Assets` tree.
- Copied Phase 1 EditMode state-machine tests into the Unity `Assets` tree.
- Copied Phase 3 rendering runtime and editor scripts into the Unity `Assets` tree.
- Added `Packages/manifest.json` with Input System, URP, Test Framework, and UGUI.
- Added `ProjectSettings/ProjectVersion.txt` pinned to Unity `6000.3.15f1`.
- Added local asset sync scripts for Windows PowerShell and macOS/Linux Bash.
- Added a Unity editor bootstrap tool:
  - `Tools/K Horror Migration/Create Bootstrap Scene`

## Unity License Blocker

An initial Unity batchmode project creation attempt reached the installed
Editor, but the Editor exited before creating `Assets`, `Packages`, and
`ProjectSettings` because no valid Unity Editor license was active on this
machine.

Observed log summary:

```text
No valid Unity Editor license found. Please activate your license.
Application will terminate with return code 198
```

This is an environment/licensing blocker, not a C# migration blocker. The
manual Unity project scaffold is in place so that after Unity Hub activation,
the project can be opened directly.

## Local Asset Sync

The ambientCG source textures are not duplicated in git under the Unity project.
They are copied locally by:

```powershell
pwsh ./Tools/SyncAmbientCgAssets.ps1
```

or:

```bash
bash ./Tools/sync_ambientcg_assets.sh
```

The copied folder is ignored by `KHorrorUnity/.gitignore`:

```text
Assets/External/ambientcg/materials/
```

## Next Action In Unity

After opening `Unity_Migration/KHorrorUnity` in Unity:

1. Let Package Manager restore packages.
2. Run `Tools/K Horror Migration/Create Bootstrap Scene`.
3. Run `Tools/K Horror Migration/Build ambientCG Materials`.
4. Open `Assets/Scenes/KHorror_Main.unity`.
5. Run EditMode tests for `KHorrorGame.Migration.Tests`.
