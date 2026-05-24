# Requirements Document

## Introduction

This spec defines the next Unity milestone for `K_horror_game`: a darker Korean rear-estate route and the first real threat AI layer.

The recommended implementation is a testable `ThreatDirector` plus simple component-based pursuit AI. It avoids early NavMesh dependency while the generated map is still changing, but it keeps the interfaces clean enough to replace movement with NavMesh later.

Implementation work stays inside `Unity_Migration/KHorrorUnity` except for Kiro spec updates.

## Requirements

### Requirement 1: Deep shrine route atmosphere

**User Story:** As a player, I want the path from the main house to the shrine to feel like a dark Korean hanok rear compound so that reaching the best item feels tense and intentional.

#### Acceptance Criteria

1. WHEN the player leaves the main house rear route THEN the route SHALL pass through Korean architectural anchors such as roofed wall gates, plaster walls, wooden beams, paper doors, eaves, lanterns, talismans, jangseung, bamboo, stone basins, or storehouses.
2. WHEN the player looks toward the shrine from the main courtyard THEN the shrine SHALL NOT be visually readable as a nearby open shortcut.
3. WHEN the player walks the rear route THEN occluders SHALL create at least three turns before the shrine entrance.
4. WHEN the route is dark THEN lanterns, shrine candles, moon bounce, and flashlight SHALL keep traversal readable.
5. IF new props are decorative THEN their colliders SHALL NOT block the intended player route unless they are named and tested as route blockers.

### Requirement 2: ThreatDirector spawn budget

**User Story:** As a player, I want threat intensity to rise with resentment so that looting deeper items makes the run more dangerous.

#### Acceptance Criteria

1. WHEN resentment stage is 0 THEN the director SHALL spawn no aggressive monsters.
2. WHEN resentment stage is 1 THEN the director SHALL allow at most one light-pressure threat.
3. WHEN resentment stage is 2 THEN the director SHALL allow at most two active threats.
4. WHEN resentment stage is 3 or higher THEN the director SHALL allow at least three active threats and increase pattern variance.
5. WHEN shrine grace is active THEN the director SHALL suppress aggressive ghost spawning.
6. IF active threat count already meets the stage budget THEN the director SHALL return no spawn request.

### Requirement 3: Territory-aware threat selection

**User Story:** As a player, I want ghosts to stay in the estate and dokkaebi to stay in the forest so that monster behavior feels fair and folklore-specific.

#### Acceptance Criteria

1. WHEN the player is in `EstateInterior` and the director can spawn THEN ghost spawn requests SHALL be valid.
2. WHEN the player is in `ForestApproach` and the director can spawn THEN dokkaebi spawn requests SHALL be valid.
3. WHEN the player is outside an enemy's allowed territory THEN that enemy SHALL not receive a chase order across the boundary.
4. WHEN the game is not in `JonggaEstate` THEN the director SHALL return no spawn request.
5. IF territory is unknown THEN the director SHALL choose a cue-only or no-op result instead of spawning.

### Requirement 4: Simple enemy AI loop

**User Story:** As a player, I want monsters to patrol, stalk, chase, and attack instead of appearing as static props.

#### Acceptance Criteria

1. WHEN an enemy is inactive THEN it SHALL remain idle and not damage the player.
2. WHEN the player is inside detection range and valid territory THEN the enemy SHALL enter `Stalk` or `Chase`.
3. WHEN the enemy reaches attack range THEN it SHALL apply damage through a player health component or testable damage receiver.
4. WHEN the target leaves valid territory THEN the enemy SHALL enter `ReturnHome`, `Lurk`, or `Despawn` instead of crossing.
5. WHEN the enemy updates movement THEN it SHALL use simple vector steering and collision-friendly speeds until NavMesh is introduced.

### Requirement 5: Stage-scaled aggression and patterns

**User Story:** As a player, I want high resentment stages to change monster count, damage, and unpredictability so that danger escalates beyond simple speed increases.

#### Acceptance Criteria

1. WHEN stage increases THEN enemy damage SHALL not decrease.
2. WHEN stage increases THEN enemy detection range, chase speed, or persistence SHALL not decrease.
3. WHEN stage is high THEN enemies SHALL gain pattern variance such as pause, burst, feint retreat, or side-step behavior.
4. WHEN variance is calculated THEN it SHALL be deterministic in tests through a provided seed or clock abstraction.
5. IF the player has just collected a shrine item THEN variance SHALL wait until grace expires before immediate attack behavior.

### Requirement 6: Scene integration and QA

**User Story:** As a developer, I want generated scene checks to prove the AI and route changes exist so future map edits do not erase them.

#### Acceptance Criteria

1. WHEN `KHorror_Main` is generated THEN it SHALL include threat director runtime objects and spawn anchors for ghost and dokkaebi.
2. WHEN the scene contains the rear route THEN tests SHALL verify required hanok route anchors and occluders exist.
3. WHEN the scene contains threat actors THEN tests SHALL verify they start hidden or inactive until the director activates them.
4. WHEN EditMode tests run THEN domain AI tests and scene integrity tests SHALL pass headlessly.
5. WHEN a task is completed THEN changes SHALL be committed and pushed to `origin/unity-conversion`.
