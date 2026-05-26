# Implementation Plan

- [x] H9. Dark hanok shrine approach pass
  - Add scene integrity tests for hanok-style rear route anchors, three sightline-breaking turns, and readable light anchors.
  - Extend `KHorrorBootstrapSceneBuilder` with roofed rear gates, wall caps, paper charms, dark eaves, lantern pools, and denser Korean props along the main-house-to-shrine route.
  - Regenerate `KHorror_Main` and verify full EditMode tests pass.
  - Commit and push to `origin/unity-conversion`.
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 6.2_

- [x] H10. ThreatDirector domain model
  - Add pure C# `ThreatDirector`, `ThreatDirectorContext`, `ThreatDirectorDecision`, `ThreatDirectorAction`, and `ThreatStageProfile`.
  - Add EditMode tests for stage budget, shrine grace suppression, territory-aware ghost/dokkaebi selection, no-op outside estate, and monotonic stage profiles.
  - Keep runtime scene behavior unchanged except for compiled domain availability.
  - Commit and push to `origin/unity-conversion`.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.4, 3.5, 5.1, 5.2_

- [x] H11. Simple enemy AI and damage loop
  - Add `EnemyBrain`, `EnemyBrainState`, `EnemyStats`, and `PlayerDamageReceiver`.
  - Add EditMode tests for idle, detection, chase, attack damage, target territory fallback, and stage-scaled stats.
  - Use simple vector steering and deterministic pattern selection; do not add NavMesh dependency yet.
  - Commit and push to `origin/unity-conversion`.
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4_

- [x] H12. Runtime threat scene integration
  - Add `RuntimeThreatSpawner` that reads `GameLoopController`, evaluates `ThreatDirector`, activates hidden generated actors, and assigns their `EnemyBrain` target/home data.
  - Add generated ghost and dokkaebi spawn anchors in valid territories.
  - Replace `ThreatProxySpawner` usage or wrap it so spawned actors are real AI actors instead of static reveal props.
  - Add scene tests proving director object, spawn anchors, inactive actors, and territory-specific actor placement.
  - Regenerate `KHorror_Main`, verify full EditMode tests pass, then commit and push.
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 6.1, 6.3, 6.4, 6.5_

- [x] H13. Pattern variance escalation
  - Add deterministic pattern variance to `EnemyBrain` using stage profile values.
  - Add pause, burst, feint retreat, and side-step pattern windows.
  - Add tests proving higher stages unlock more variance without immediate attack during shrine grace.
  - Commit and push to `origin/unity-conversion`.
  - _Requirements: 5.3, 5.4, 5.5_

- [x] H14. Stage-scaled runtime threat pools
  - Add regression tests proving stage-five budgets request additional ghost and dokkaebi actors while budget remains.
  - Expand `RuntimeThreatSpawner` from one ghost and one dokkaebi into serialized actor and anchor pools.
  - Regenerate `KHorror_Main` with three interior ghost actors and two forest dokkaebi actors, all hidden until activated.
  - Verify full EditMode tests pass and push to `origin/unity-conversion`.
  - _Requirements: 2.3, 2.4, 3.1, 3.2, 4.1, 5.1, 5.3, 6.1, 6.3, 6.4_
