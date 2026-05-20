# Phase 1 Core Loop Migration

## Scope

Godot source files were read only as references. No `.gd` or `.tscn` files were renamed, overwritten, or migrated in place.

Reference files analyzed:

- `scripts/game/main.gd`
- `scripts/core/inventory.gd`
- `scripts/core/artifact_definition.gd`
- `scripts/core/quota_tracker.gd`
- `scripts/core/resentment_tracker.gd`
- `scripts/player/player_controller.gd`
- `scripts/player/interactor.gd`
- `scripts/props/artifact.gd`
- `scripts/zones/extraction_zone.gd`
- `scripts/interactions/bongo_quota_monitor.gd`
- `scripts/interactions/bongo_settlement_station.gd`

## Output Tree

```text
Unity_Migration/
  Phase1_CoreLoop/
    Scripts/
      KHorrorGame.Migration.asmdef
      Domain/
        ArtifactDefinition.cs
        BongoRunStateMachine.cs
        GameMapId.cs
        Inventory.cs
        QuotaTracker.cs
        ResentmentTracker.cs
        ThreatSpawnGate.cs
      Runtime/
        GameLoopController.cs
        Interaction/
          ArtifactPickup.cs
          BongoTerminal.cs
          ExtractionZone.cs
          IInteractable.cs
          PlayerInteractor.cs
          SettlementStation.cs
          StatefulInteractable.cs
        Player/
          UnityPlayerController.cs
        UI/
          BongoMonitorDisplay.cs
          HudPresenter.cs
    Tests/
      EditMode/
        BongoRunStateMachineTests.cs
        KHorrorGame.Migration.Tests.asmdef
```

## Unity Architecture

- `BongoRunStateMachine` is a pure C# state machine for the run loop: van hub, Jongga estate, cargo loading, hub return, settlement office, and cargo settlement.
- `GameLoopController` is the scene-level MonoBehaviour bridge. It owns quota, resentment, threat spawn grace, travel timing, player teleport anchors, and map root activation.
- `UnityPlayerController` ports the first-person controller to Unity `CharacterController` plus Input System actions, with keyboard fallback for WASD, Space, Shift, E, and Q.
- `PlayerInteractor` replaces Godot `RayCast3D` interaction with `Physics.Raycast` from the camera.
- `ArtifactPickup`, `ExtractionZone`, `BongoTerminal`, and `SettlementStation` replace the main interactable gameplay nodes.
- `Inventory` enforces two hand slots: one large two-hand item or two small one-hand items.
- `ThreatSpawnGate` keeps shrine items from spawning the threat immediately by adding an 8 second grace window before hostile spawning is allowed.

## Flow Mapping

```text
Van Hub
  E on BongoTerminal -> travel to Jongga Estate

Jongga Estate
  E on ArtifactPickup -> add item to hands, add resentment, start shrine grace if needed
  E on ExtractionZone -> move player inventory into pending van cargo
  E on BongoTerminal -> return to Van Hub

Van Hub with pending cargo
  E on BongoTerminal -> travel to Settlement Office

Settlement Office
  E on SettlementStation or terminal -> settle pending cargo into quota
  E on BongoTerminal with no pending cargo -> return to Van Hub
```

## Verification

Edit mode tests cover:

- terminal travel from hub to estate
- cargo extraction only after reaching estate
- settlement only after returning to hub and travelling to the settlement office
- two-hand inventory limits
- shrine item threat grace timing

Local text verification:

- `git diff --check` passed after marking the new files as intent-to-add.
- Non-ASCII and broken replacement character scans returned no matches in `Unity_Migration/`.
- Pure domain C# files compile through local PowerShell `Add-Type`.
- The local shell does not have `dotnet` or `csc` on `PATH`, and this repository is not a Unity project root, so Unity EditMode tests were authored but not executed here.

These scripts are ready to be copied into a Unity project with the Input System and Unity UI packages enabled.
