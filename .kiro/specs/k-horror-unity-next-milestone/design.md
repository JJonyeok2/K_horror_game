# Design Document

## Overview

The next milestone turns the Unity migration from a generated playable prototype into a structured horror retrieval slice. The work is organized around five pillars:

- The bongo-to-estate-to-settlement loop must be reliable.
- The front gate must be a player transition boundary, not an open physical doorway.
- Ghosts must be constrained to estate territory, while dokkaebi can operate in the forest.
- The forest, courtyard, main house, and shrine must be large and readable enough to support tension.
- Visuals, prompts, and QA must make each build testable without guessing what is broken.

The current bootstrap scene builder is useful for fast iteration, but the design should move toward data-driven components and prefabs as the map becomes richer.

## Current Baseline

The Unity project currently contains:

- `BongoRunStateMachine` for hub, estate, travel, cargo, and settlement state.
- `GameLoopController` for scene root activation and player teleporting between map states.
- `UnityPlayerController` for movement, sprint, jump, stamina, inventory, and item dropping.
- `IInteractable`, `PlayerInteractor`, terminal, extraction, pickup, and gate portal interactions.
- `ThreatSpawnGate` for shrine item grace behavior.
- `KHorrorBootstrapSceneBuilder` for the generated bongo hub, forest approach, gate, courtyard, main house, shrine, lighting, materials, and HUD.
- EditMode tests covering the run state machine and gate portal.

The missing major systems are enemy AI, territory enforcement, richer level topology, stronger interaction feedback, and automated map integrity checks.

## Architecture

```mermaid
flowchart TD
    "Player" --> "PlayerInteractor"
    "PlayerInteractor" --> "IInteractable"
    "IInteractable" --> "BongoTerminal"
    "IInteractable" --> "EstateGatePortal"
    "IInteractable" --> "ArtifactPickup"
    "IInteractable" --> "ExtractionZone"
    "BongoTerminal" --> "GameLoopController"
    "ExtractionZone" --> "GameLoopController"
    "GameLoopController" --> "BongoRunStateMachine"
    "ArtifactPickup" --> "ResentmentTracker"
    "ResentmentTracker" --> "ThreatDirector"
    "ThreatDirector" --> "EnemySpawner"
    "EnemySpawner" --> "EnemyController"
    "EnemyController" --> "TerritoryResolver"
    "TerritoryResolver" --> "TerritoryVolume"
    "EstateGatePortal" --> "TerritoryBoundary"
```

## Components and Interfaces

### Game loop

Keep `BongoRunStateMachine` as the pure C# authority for map state, cargo state, and quota state. Runtime Unity objects should call into it through `GameLoopController`.

Planned additions:

- `RunPhasePresenter`: shows current run phase without bloating `HudPresenter`.
- `BongoTabletPanel`: owns terminal UI state and action text.
- `TravelSequenceController`: handles short travel animation, audio, and fade before `GameLoopController` teleports the player.

### Interaction

`PlayerInteractor` remains the single raycast source. All interactables implement `IInteractable`.

Planned additions:

- `InteractionPromptData`: label, subdued target name, invalid reason, and optional hold duration.
- `PromptPresenter`: center-bottom prompt rendering, separate from status text.
- `InteractableFeedback`: optional sound, UI flash, or animation event when an action succeeds or fails.

### Inventory and cargo

Keep the one-item-or-two-small-items hand-slot rule. The inventory model should stay testable without Unity objects.

Planned additions:

- `HeldItemViewDefinition`: prefab, local offset, local scale, hand slot count.
- `DroppedArtifactView`: pooled dropped pickup object with definition data restored.
- Failure feedback for full hands, too-heavy items, and invalid cargo loading.

### Territory system

Enemy territory needs to be explicit and testable. The gate cannot be a vague coordinate check spread across AI scripts.

Planned runtime model:

```csharp
public enum EnemyKind
{
    Ghost,
    Dokkaebi
}

public enum TerritoryKind
{
    BongoHub,
    ForestApproach,
    EstateInterior,
    SettlementOffice
}

public sealed class TerritoryVolume : MonoBehaviour
{
    public TerritoryKind Territory;
}

public sealed class EnemyTerritoryRules
{
    public EnemyKind Kind;
    public IReadOnlySet<TerritoryKind> AllowedTerritories;
}
```

Rules:

- `Ghost`: `EstateInterior` only.
- `Dokkaebi`: `ForestApproach` only for the first milestone.
- Player gate portals do not imply AI traversal.
- If an enemy target leaves allowed territory, the enemy switches to `ReturnHome`, `Lurk`, or `Despawn`.

### Enemy AI

Use a small state machine before adding complex behavior trees.

Ghost states:

- `Dormant`: exists but inactive.
- `Haunt`: creates cues inside estate.
- `Investigate`: moves toward sound or last known player position.
- `Stalk`: maintains distance and line-of-sight pressure.
- `Chase`: direct pursuit while player remains inside estate.
- `ReturnHome`: target invalid or player escaped through gate.
- `Despawn`: cleanup when outside valid scene scope.

Dokkaebi states:

- `Lurk`: waits in forest volume.
- `Misdirect`: plays lure cue or appears briefly.
- `BlockPath`: pressures a route without hard-locking progress.
- `Retreat`: leaves when player enters the estate or returns to bongo.

### Threat director

`ThreatSpawnGate` should remain the small grace-window model, but a new `ThreatDirector` should own enemy spawn decisions.

Inputs:

- Current `GameMapId`.
- Current `TerritoryKind` for player.
- Resentment stage.
- Held or recovered artifact tags.
- Shrine grace timer.
- Active enemy count.

Outputs:

- Spawn ghost in estate interior.
- Spawn dokkaebi in forest.
- Play cue only.
- Do nothing during grace or invalid territory.

### Level structure

The generated scene should be split conceptually into volumes:

```text
World
+- BongoHub
+- JonggaEstate
|  +- ForestApproach
|  +- FrontGateBoundary
|  +- Courtyard
|  +- MainHouse
|  +- BackRoute
|  +- Shrine
+- SettlementOffice
+- BongoTravel
```

The builder can continue generating a test scene, but each area should be refactored toward:

- A parent `GameObject` per zone.
- `TerritoryVolume` colliders per zone.
- Stable spawn markers for player and enemies.
- Occluders and collision blockers named by purpose.
- Reusable prefab-like helper methods for trees, jangseung, doors, roof modules, walls, props, and light fixtures.

### Visuals and rendering

Use the existing ambientCG material pipeline for PBR materials. The next pass should focus on Korean identity and visibility:

- Bark and canopy variation for taller forest.
- Packed earth, mud, stone threshold, plaster, dark wood, roof tile materials.
- Jangseung, sotdae, talismans, jangdok, shrine rope, paper doors, and deep eaves as recurring shape language.
- Lanterns and moonlight as navigation anchors.
- Fog and post-processing tuned to be dark but playable.

### Audio

Audio can start with placeholder clips and event names so implementation remains unblockable.

Suggested event channels:

- `Gate.OpenTransition`
- `Gate.ReturnTransition`
- `Forest.DokkaebiCue`
- `Estate.GhostNear`
- `Estate.ResentmentStageUp`
- `Cargo.Loaded`
- `Terminal.ActionAccepted`
- `Terminal.ActionDenied`

## Data Flow

1. Player uses bongo terminal.
2. `BongoTerminal` calls `GameLoopController.OperateBongoTerminal`.
3. `BongoRunStateMachine` enters travel, then estate.
4. Player walks through forest territory.
5. `ThreatDirector` may activate dokkaebi if resentment and route rules allow it.
6. Player uses `EstateGatePortal`.
7. Territory changes to estate interior.
8. Ghost rules become eligible after grace, resentment, and spawn checks.
9. Player picks up artifacts, resentment changes, inventory updates.
10. Player returns through gate or route to bongo return area.
11. Extraction stores cargo, then hub terminal routes to settlement.

## Error Handling

- Missing portal spawn references: disable interaction and log a clear error.
- Missing territory volume: treat as unsafe neutral territory and prevent enemy spawning.
- Enemy target outside allowed territory: switch to fallback state, never force movement across boundary.
- Missing material or texture: use fallback material and log once per material name.
- Invalid terminal state: show unavailable prompt and do not change map state.
- Broken map integrity check: fail tests with object name and expected bounds.

## Testing Strategy

### EditMode tests

- State machine travel, extraction, settlement, and quota.
- Inventory hand slots and drop restoration.
- Gate portal inside/outside behavior.
- Territory rules for ghost and dokkaebi.
- ThreatDirector spawn decisions with grace timer and territory inputs.

### PlayMode tests

- Player can raycast and interact with gate, terminal, pickups, extraction, and settlement.
- Player cannot fall through known gate, courtyard, main house, shrine, or approach floor zones.
- Ghost cannot cross from estate interior to forest approach.
- Dokkaebi can spawn in forest approach and despawns or retreats at the gate.

### Scene integrity checks

- Required roots exist.
- Required spawn markers exist.
- Required gate portal references are assigned.
- Required zone floors and major colliders exist.
- Known seam blockers and sightline blockers exist at the front gate.
- No major route segment has a missing floor volume.

## Implementation Order

1. Add territory data model and tests.
2. Add ThreatDirector and enemy spawn decision tests.
3. Add ghost and dokkaebi MVP controllers with simple placeholder visuals.
4. Add territory volumes to the generated scene.
5. Expand forest, courtyard, main house, back route, and shrine structure.
6. Improve prompts, bongo tablet UI, and inventory feedback.
7. Improve PBR material assignment, lighting, fog, and visibility.
8. Add audio event hooks and placeholder cues.
9. Add PlayMode and scene integrity tests.
10. Regenerate the scene, run tests, and push to `unity-conversion`.
