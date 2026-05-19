# K_horror_game

Godot 4 prototype for a Korean horror retrieval game.

## Current Branch

Use `k-horror-mvp2` for the current playable MVP build.

## Run on macOS

1. Install Godot 4.6.x from https://godotengine.org/download/macos/.
2. Clone the repository and switch to the branch:

```bash
git clone https://github.com/JJonyeok2/K_horror_game.git
cd K_horror_game
git switch k-horror-mvp2
```

3. Open the project in Godot and run the main scene.

The project uses repository-relative `res://` paths only. The external PBR materials are checked in under `assets/external/ambientcg/materials`, so no Windows-only paths or local downloads are required on macOS.

## Test

If the Godot executable is available as `godot`, run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
godot --headless --path . --script res://tests/scene/test_external_materials.gd
godot --headless --path . --script res://tests/scene/test_playable_scene.gd
```
