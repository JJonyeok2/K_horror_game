# Implementation Plan

- [x] H1. Hotfix bongo readability and playable brightness
  - Replace the visible green extraction debug box with an invisible trigger inside a visible estate return bongo.
  - Brighten moonlight, flashlight, fill lights, exposure, and fog for readable playtesting.
  - Regenerate `KHorror_Main` and verify EditMode tests still pass.
  - _Requirements: 6.3, 7.4, 7.6_

- [x] H2. Hotfix estate return and rear route continuity
  - Add a visible return bongo interaction that can return without cargo or load cargo then return.
  - Open the main house front entry and rear exit connector toward the shrine path.
  - Add first-pass sarangchae and kitchen shed structures behind the courtyard.
  - Move shrine pickup onto the accessible shrine floor area.
  - _Requirements: 1.3, 1.4, 4.4, 4.5, 4.6, 6.3_

- [x] H3. Hotfix playable floor continuity
  - Add regression tests for walkable ground at bongo apron, gate, courtyard, main house, rear route, and shrine connectors.
  - Fill the bongo-to-forest apron gap and add a continuous underlay to prevent falls through visual seams.
  - Rebuild the main house rear exit with traversable landing and step heights.
  - Regenerate `KHorror_Main` and verify the full EditMode suite passes.
  - _Requirements: 4.1, 4.4, 4.5, 4.6, 9.3_

- [x] H4. Hotfix estate escape boundaries
  - Add regression tests for south, west, east, and north escape-blocking boundaries at standing and jump height.
  - Add map boundary ridges around the approach, forest, courtyard, rear route, and shrine playable envelope.
  - Regenerate `KHorror_Main` and verify the full EditMode suite passes.
  - _Requirements: 4.1, 4.3, 4.5, 4.6, 9.3_

- [x] H5. Hotfix front gate wall seams
  - Add a regression test proving the player cannot bypass the front gate through either gate-wall seam.
  - Connect the front gate posts to the adjacent stone walls with blocking wall segments.
  - Regenerate `KHorror_Main` and verify the full EditMode suite passes.
  - _Requirements: 2.1, 2.2, 4.6, 4.7, 9.3_

- [x] H6. Hotfix shrine access, loot density, and visible threat spawn
  - Make shrine entrance cloth and rope decorations visual-only so they do not block the player capsule.
  - Increase estate pickup artifacts from two to seven across courtyard, main house, sarangchae, kitchen shed, and shrine.
  - Add visible ghost and dokkaebi threat proxies driven by the threat gate and resentment stage.
  - Add regression tests for shrine decoration collision, artifact count, and threat proxy activation.
  - Regenerate `KHorror_Main` and verify the full EditMode suite passes.
  - _Requirements: 3.6, 4.5, 4.6, 5.1, 8.1, 8.2, 8.3, 9.3_

- [x] H7. Hotfix shrine approach route funnel
  - Add a regression test proving the shrine entrance cannot be approached directly from east or west side shortcuts.
  - Add stone and bamboo route boundaries so the shrine entrance is reached from the intended back route.
  - Keep the central back shrine path open and regenerate `KHorror_Main`.
  - Verify the full EditMode suite passes.
  - _Requirements: 4.5, 4.6, 4.7, 7.3, 9.3_

- [x] H8. Hotfix deep shrine route and rear estate density
  - Add regression tests for deep rear shrine placement, best Jongga loot placement, main house side shortcut blockers, and rear route density.
  - Move the shrine to the rear of the estate and place the highest-value Jongga spirit tablet there.
  - Add a layered rear compound with zigzag walls, storehouse, side room, garden props, lanterns, bamboo, and jangseung.
  - Extend estate ground, lighting, and boundaries for the deeper route.
  - Regenerate `KHorror_Main` and verify the full EditMode suite passes.
  - _Requirements: 3.6, 4.5, 4.6, 4.7, 7.1, 7.3, 9.3_

- [x] 1. Establish territory foundations
  - Add `EnemyKind` and `TerritoryKind` domain enums.
  - Add a testable `EnemyTerritoryRules` service that answers whether a kind can enter a territory.
  - Add EditMode tests for ghost-only estate territory and dokkaebi-only forest territory.
  - _Requirements: 2.6, 3.1, 3.3, 3.5, 9.2_

- [x] 2. Add Unity territory volumes
  - Add `TerritoryVolume` MonoBehaviour with serialized `TerritoryKind`.
  - Add `TerritoryResolver` that resolves a world position or collider overlap to a territory.
  - Add safe fallback behavior for missing or overlapping volumes.
  - _Requirements: 3.5, 4.6, 9.2_

- [x] 3. Wire territory volumes into the generated scene
  - Update `KHorrorBootstrapSceneBuilder` to create parent objects for `ForestApproach`, `FrontGateBoundary`, `Courtyard`, `MainHouse`, `BackRoute`, and `Shrine`.
  - Add trigger volumes for forest approach and estate interior.
  - Keep the front gate portal player-only and block AI traversal by default.
  - _Requirements: 2.1, 2.2, 2.6, 3.1, 3.4_

- [ ] 4. Build the ThreatDirector
  - Add `ThreatDirector` that reads current map, player territory, resentment, grace timer, and active enemy count.
  - Preserve `ThreatSpawnGate` shrine grace behavior.
  - Add tests for no spawn during grace, ghost spawn inside estate, and dokkaebi spawn in forest.
  - _Requirements: 3.6, 8.1, 8.5, 9.1, 9.2_

- [ ] 5. Implement ghost MVP behavior
  - Add `EnemyController` base state machine with target tracking and territory validation.
  - Add `GhostEnemy` states: `Dormant`, `Haunt`, `Investigate`, `Stalk`, `Chase`, `ReturnHome`, `Despawn`.
  - Ensure ghosts stop, return, or despawn when the player leaves through the front gate.
  - Add PlayMode or component tests proving ghosts do not cross into forest approach.
  - _Requirements: 3.1, 3.2, 3.5, 8.2, 9.2_

- [ ] 6. Implement dokkaebi MVP behavior
  - Add `DokkaebiEnemy` states: `Lurk`, `Misdirect`, `BlockPath`, `Retreat`.
  - Spawn dokkaebi only in forest approach territory for this milestone.
  - Add a simple placeholder visual and forest cue event.
  - Ensure dokkaebi does not enter estate interior past the gate.
  - _Requirements: 3.3, 3.4, 8.3, 9.2_

- [ ] 7. Improve the bongo tablet and travel UX
  - Replace oversized world text with terminal interaction prompt plus a tablet or screen-space panel.
  - Show valid actions for depart, return, settlement, and unavailable states.
  - Add travel audio and optional fade or motion sequence before player placement.
  - _Requirements: 1.1, 1.2, 6.1, 6.2, 6.4, 6.5, 8.4_

- [ ] 8. Complete return, cargo, and settlement flow polish
  - Implement the Korean Kiro sub-spec in `.kiro/specs/k-horror-physical-cargo-loop`.
  - Replace value-only cargo extraction with visible physical cargo placed inside the return bongo.
  - Make settlement calculate from physical cargo in the bongo hold, not from an invisible pending value deposit.
  - Add success and failure feedback for cargo loading, repickup, and settlement.
  - _Requirements: 1.3, 1.4, 1.5, 5.7, 6.3_

- [ ] 9. Refine interaction prompt presentation
  - Split `HudPresenter` responsibilities so center-bottom prompts are independent from status text.
  - Show pickup key and item name in subdued text.
  - Show invalid reasons for full hands or invalid actions.
  - _Requirements: 5.1, 5.2, 5.5, 6.5_

- [ ] 10. Strengthen inventory and dropped item handling
  - Add held-item view definitions for small and large artifacts.
  - Ensure one large item uses both hands and two small items use left/right hands.
  - Ensure dropped items and bongo cargo items restore pickup definitions and do not spawn under the floor.
  - Keep hand-held, dropped, and bongo-loaded states mutually exclusive for each artifact.
  - _Requirements: 5.3, 5.4, 5.5, 5.6_

- [ ] 11. Expand the forest approach route
  - Increase playable path distance so normal walking from bongo to gate takes at least 60 seconds.
  - Add taller trees, canopy clusters, jangseung pairs, rocks, dead grass, and fog occluders.
  - Add route blockers that shape the path without catching the player on small steps.
  - Add a timed traversal check or documented measurement pass.
  - _Requirements: 4.1, 4.2, 7.2_

- [ ] 12. Rebuild the courtyard as a navigable space
  - Add route anchors such as well, jangdok area, sheds, stacked wood, low walls, and lantern points.
  - Reduce empty open-lot feel by placing collision-safe props and occluders.
  - Keep route readability from gate to main house and optional side routes.
  - _Requirements: 4.3, 4.6, 7.3_

- [ ] 13. Build the main house interior route
  - Add floors, ceilings, rooms, door frames, paper doors, beams, and collision.
  - Make at least one artifact route inside the house.
  - Add path constraints and line-of-sight blockers without creating dead-end traps.
  - _Requirements: 4.4, 4.6, 7.3_

- [ ] 14. Build the shrine and back route
  - Add a readable route from main house or courtyard to shrine.
  - Ensure shrine artifacts sit above the floor and have valid pickup colliders.
  - Preserve shrine threat grace before immediate aggressive spawning.
  - _Requirements: 3.6, 4.5, 4.6_

- [ ] 15. Define side path risk
  - Keep front gate as mandatory main entry.
  - Add optional side paths that are riskier, narrower, and more exposed to dokkaebi or sound events.
  - Prevent side path collision gaps from bypassing the intended gate boundary.
  - _Requirements: 4.7, 2.6, 3.4_

- [ ] 16. Upgrade Korean horror visual identity
  - Apply ambientCG PBR materials to ground, bark, stone, plaster, roof, and wood.
  - Add Korean details: roof eaves, paper doors, jangseung faces, sotdae, talismans, jangdok, shrine rope, and worn plaster.
  - Replace clay-like primitives where feasible with higher fidelity composed meshes or prefabs.
  - _Requirements: 7.1, 7.2, 7.3_

- [ ] 17. Tune lighting and visibility
  - Keep low-spec mode off for development.
  - Tune flashlight range, moonlight, lantern anchors, exposure, fog, shadows, and post-processing.
  - Verify the scene is dark but navigable in forest, gate, courtyard, house, and shrine.
  - _Requirements: 7.4, 7.5, 7.6_

- [ ] 18. Add audio event hooks
  - Add event hooks for gate transition, terminal accepted or denied, cargo loaded, resentment stage up, ghost nearby, and dokkaebi cue.
  - Use placeholders if final audio assets are not ready.
  - Prevent aggressive attack cues when grace or territory rules block spawning.
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 19. Add scene integrity tests
  - Add checks for required roots, spawn markers, gate portal references, sightline blockers, and core floors.
  - Add known hole-zone checks around gate, courtyard, house, shrine, and return route.
  - Run these checks in batchmode before committing scene generation changes.
  - _Requirements: 2.4, 2.5, 4.6, 9.3_

- [ ] 20. Add PlayMode smoke tests
  - Test player interaction with terminal, gate, pickup, drop, extraction, and settlement.
  - Test enemy territory boundaries for ghost and dokkaebi.
  - Test route collision around the gate and main estate path.
  - _Requirements: 1.6, 2.1, 2.2, 3.2, 3.4, 9.1, 9.2_

- [ ] 21. Regenerate and verify the Unity scene
  - Run `Tools/K Horror Migration/Create Bootstrap Scene` after builder changes.
  - Run EditMode tests and PlayMode or scene integrity tests.
  - Check `git diff --check` before staging Unity YAML.
  - _Requirements: 9.1, 9.3, 9.5_

- [ ] 22. Publish milestone branch updates
  - Commit logical groups by system: territory and AI, level pass, UI and inventory, visual pass, tests.
  - Push updates to `origin/unity-conversion`.
  - Keep Godot `.gd` and `.tscn` files reference-only unless the user explicitly requests Godot edits.
  - _Requirements: 9.5, 10.1, 10.2, 10.4_
