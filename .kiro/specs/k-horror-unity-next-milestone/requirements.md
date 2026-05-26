# Requirements Document

## Introduction

This spec defines the next development milestone for the Unity version of `K_horror_game`.
The goal is to move the current Unity prototype from a generated graybox scene into a playable Korean horror retrieval loop with clear map boundaries, enemy territory rules, readable interactions, and a higher quality forest and estate presentation.

The existing Godot project remains reference-only. New implementation work should happen inside `Unity_Migration/KHorrorUnity` unless a task explicitly updates documentation or shared asset credits.

## Requirements

### Requirement 1: Lethal Company-style run flow

**User Story:** As a player, I want to start in the bongo hub, travel to the estate, retrieve items, return to the bongo, and settle cargo so that the core run loop feels complete.

#### Acceptance Criteria

1. WHEN the player starts a run THEN the player SHALL begin inside the 1990s bongo hub.
2. WHEN the player uses the bongo terminal THEN the game SHALL move through a short travel state before placing the player at the estate approach.
3. WHEN the player extracts cargo from the estate THEN the game SHALL store recovered value separately from settled quota.
4. WHEN recovered cargo exists at the hub THEN the terminal SHALL route the player to the settlement office.
5. WHEN settlement is completed THEN recovered cargo SHALL be added to quota and cleared from pending cargo.
6. IF travel is already active THEN terminal actions SHALL be ignored until travel completes.

### Requirement 2: Gate transition and map boundary

**User Story:** As a player, I want the front gate to behave like a real map transition rather than a pass-through doorway so that entering the estate feels intentional and controlled.

#### Acceptance Criteria

1. WHEN the player approaches the closed front gate from outside THEN a center-bottom interaction prompt SHALL appear.
2. WHEN the player interacts with the front gate from outside THEN the player SHALL be moved to an inside estate spawn point.
3. WHEN the player interacts with the front gate from inside THEN the player SHALL be moved back to the exterior gate approach.
4. WHEN the player looks at the closed gate seam THEN the player SHALL NOT be able to see through into the courtyard.
5. WHEN the player stands below or behind the front gate THEN the player SHALL remain on continuous collision floor and SHALL NOT fall through the map.
6. IF a non-player enemy reaches the front gate THEN the enemy SHALL NOT use the player gate portal unless explicitly allowed by its territory rules.

### Requirement 3: Enemy territory rules

**User Story:** As a player, I want ghosts to be trapped inside the estate while dokkaebi can exist in the forest so that each threat has a clear folklore identity and fair boundaries.

#### Acceptance Criteria

1. WHEN an enemy is classified as `Ghost` THEN it SHALL only spawn and navigate inside the estate territory.
2. WHEN a `Ghost` chases the player and the player exits through the gate THEN the ghost SHALL stop at the gate boundary, return home, or despawn according to its state.
3. WHEN an enemy is classified as `Dokkaebi` THEN it MAY spawn and navigate in the forest approach territory.
4. WHEN a `Dokkaebi` reaches the front gate THEN it SHALL NOT enter the estate interior unless a later task explicitly grants that behavior.
5. WHEN an enemy target is outside its allowed territory THEN the enemy SHALL choose a fallback state instead of crossing the boundary.
6. WHEN shrine-class artifacts are collected THEN ghost spawning SHALL respect the existing grace window before aggressive pursuit begins.

### Requirement 4: Estate and approach level quality

**User Story:** As a player, I want the path from the bongo to the gate and the estate interior to feel large, dense, and navigable so that the map supports exploration and tension.

#### Acceptance Criteria

1. WHEN walking from the bongo to the front gate without sprinting THEN the route SHALL take at least 60 seconds in normal movement tuning.
2. WHEN the player walks the approach path THEN dense forest, tall trees, jangseung, grass, rocks, and occluding silhouettes SHALL break up sightlines.
3. WHEN the player enters through the front gate THEN the courtyard SHALL include readable route anchors, obstacles, and points of interest instead of a mostly empty open lot.
4. WHEN the player enters the main house THEN the house SHALL include an actual interior route with floors, walls, doors, ceilings, and navigation constraints.
5. WHEN the player seeks the shrine THEN a back route from the main house or courtyard SHALL exist and SHALL be readable without being fully exposed from the front.
6. WHEN the player tests the map boundaries THEN floors, pillars, walls, doors, and props SHALL have collision that prevents falling through or clipping through major structures.
7. IF side paths exist THEN they SHALL be optional, riskier than the main gate route, and clearly separated from the mandatory front gate entry.

### Requirement 5: Interaction and inventory readability

**User Story:** As a player, I want interaction prompts and carried items to be readable without blocking the whole screen so that looting and returning cargo feel responsive.

#### Acceptance Criteria

1. WHEN the player looks at an interactable object THEN the prompt SHALL appear near the center-bottom of the screen.
2. WHEN the player looks at a pickup THEN the prompt SHALL include the item name in subdued text.
3. WHEN the player picks up a small item THEN it SHALL occupy one hand slot and appear in a left or right held position.
4. WHEN the player picks up a large item THEN it SHALL occupy both hand slots and appear as a two-hand held object.
5. WHEN the player already has full hand slots THEN additional pickups SHALL be rejected with clear feedback.
6. WHEN the player presses the drop command THEN the currently held item SHALL be placed back into the world as an interactable pickup.
7. WHEN cargo is loaded into the return zone THEN the player's hands SHALL clear and the recovered value SHALL update.

### Requirement 6: Bongo tablet and return flow

**User Story:** As a player, I want the bongo tablet to be the central control surface for travel, return, and settlement so that hub actions do not float as oversized world text.

#### Acceptance Criteria

1. WHEN the player looks at the bongo terminal THEN the terminal SHALL show an interaction prompt instead of oversized 3D text outside the tablet.
2. WHEN the player interacts with the terminal THEN a screen-space or tablet-attached UI SHALL show the available action.
3. WHEN the player wants to return from the estate THEN the player SHALL enter or approach the bongo return area and use a return or cargo trigger.
4. WHEN the player has recovered cargo THEN the hub terminal SHALL offer the settlement route before another estate run.
5. IF no action is valid THEN the terminal SHALL show a short unavailable state instead of silently failing.

### Requirement 7: Korean horror visual quality

**User Story:** As a player, I want the world to look like a dark Korean rural estate rather than clay blocks so that the horror atmosphere is carried by materials, forms, and lighting.

#### Acceptance Criteria

1. WHEN the Unity scene is generated THEN PBR materials from the ambientCG import pipeline SHALL be applied to ground, bark, stone, plaster, roof, and wood surfaces where available.
2. WHEN the player walks the forest THEN tree scale, bark texture, canopy density, fog, and ground variation SHALL read as a forest rather than roadside props.
3. WHEN the player sees estate buildings THEN roof tiles, eaves, plaster, wood beams, paper doors, courtyard objects, jangdok, shrine elements, and talismans SHALL read as Korean.
4. WHEN the scene is dark THEN the flashlight, moonlight, lanterns, and exposure SHALL keep navigation readable.
5. WHEN low-spec mode is off in development THEN lighting, fog, shadows, and post-processing SHALL target the higher quality Unity pipeline.
6. IF visibility drops below playable levels THEN lighting profiles SHALL be adjusted before adding more darkness.

### Requirement 8: Horror feedback and audio direction

**User Story:** As a player, I want sound and subtle events to warn me before threats become aggressive so that fear feels earned rather than random.

#### Acceptance Criteria

1. WHEN resentment increases THEN environmental audio and visual events SHALL escalate in clear stages.
2. WHEN a ghost is nearby inside the estate THEN directional cues SHALL hint at proximity before direct pursuit.
3. WHEN a dokkaebi is active in the forest THEN misdirection or lure cues MAY occur outside the estate.
4. WHEN the gate transition happens THEN audio SHALL support the transition with gate, latch, or ambient shift cues.
5. IF a threat cannot spawn due to grace period or territory rules THEN the director SHALL not play misleading immediate attack cues.

### Requirement 9: QA, tests, and build confidence

**User Story:** As a developer, I want automated checks for state, territory, interaction, and mesh integrity so that repeated visual work does not reintroduce broken movement or boundaries.

#### Acceptance Criteria

1. WHEN core loop logic changes THEN EditMode tests SHALL cover travel, extraction, settlement, inventory, and threat grace behavior.
2. WHEN gate or enemy territory rules change THEN tests SHALL prove ghosts cannot leave the estate and dokkaebi can operate in the forest.
3. WHEN the generated scene changes THEN a map integrity check SHALL verify required floors, major colliders, gate portal references, and no known hole zones.
4. WHEN the project is opened on Windows or Mac THEN Unity SHALL regenerate ignored local folders without requiring committed `Library`, `Temp`, or `Logs` content.
5. WHEN changes are ready for review THEN the `unity-conversion` branch SHALL include the Unity project, generated scene, scripts, tests, and docs needed to continue development.

### Requirement 10: Scope and migration safety

**User Story:** As a developer, I want clear boundaries between Godot reference code and Unity implementation so that migration work does not corrupt the original prototype.

#### Acceptance Criteria

1. WHEN implementing new runtime behavior THEN code SHALL be written in Unity C# under `Unity_Migration/KHorrorUnity`.
2. WHEN reading Godot `.tscn` or `.gd` files THEN they SHALL be treated as reference-only unless the user explicitly asks for Godot edits.
3. WHEN adding external assets THEN source, license, and transformation notes SHALL be recorded.
4. WHEN generated Unity YAML changes are committed THEN the change SHALL be traceable to a builder, prefab, material, or scene authoring step.
5. IF a task requires workspace-external access THEN work SHALL stop and the user SHALL be asked before proceeding.
