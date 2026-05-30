# Internal Playflow Spec Status Audit

## Summary

Task 3 reconciles older Kiro task checkboxes against current Unity code, scene tests, and internal playflow proof coverage. Only items with direct code and test evidence were marked complete. Items that are only partially implemented remain unchecked.

## k-horror-unity-next-milestone

Marked complete:

- `4. Build the ThreatDirector`
  - Evidence: `Assets/KHorrorGame/Migration/CoreLoop/Scripts/Domain/ThreatDirector.cs`
  - Tests: `ThreatDirectorTests.ShrineGraceSuppressesAggressiveSpawnButKeepsCue`, `ThreatDirectorTests.EstateStageFourRequestsGhost`, `ThreatDirectorTests.ForestStageThreeRequestsDokkaebi`

- `14. Build the shrine and back route`
  - Evidence: generated `KHorror_Main.unity` contains the rear shrine route, deep rear objective, and shrine pickup placement.
  - Tests: `EstateContentIntegrityTests.RearShrineRouteHasDarkHanokAtmosphereAnchors`, `EstateContentIntegrityTests.ShrineEntranceOnlyAllowsApproachFromBackRoute`, `EstateContentIntegrityTests.BestJonggaArtifactSitsAtTheDeepRearObjective`, `GameLoopControllerTests.ShrineArtifactPickupForcesMaximumThreatStageAndStartsGrace`

- `19. Add scene integrity tests`
  - Evidence: Unity scene integrity coverage exists for roots, spawn markers, gate portal references, floors, route blockers, and known escape gaps.
  - Tests: `EstateContentIntegrityTests.EstateHasTerritoryRootsResolverAndGateAiBoundary`, `EstateFloorContinuityTests.EstateRouteHasWalkableGroundAtCriticalConnectors`, `EstateGatePortalTests.OuterGateFlanksDoNotAllowBypassAroundGate`, `EstateBoundaryIntegrityTests.EstatePlayableAreaHasEscapeBlockingBoundaries`

- `10. Strengthen inventory and dropped item handling`
  - Evidence: `UnityPlayerController.DropOrDepositCurrentArtifact` routes the same drop input to van loading only when the player is inside a `VanCargoDepositZone`; otherwise it creates a world pickup and snaps it above the walkable floor. Held views still use one-hand left/right and two-hand centered obstruction layouts.
  - Tests: `VanCargoDepositZoneTests.CargoDropInputInsideVanZoneDepositsInsteadOfDroppingLooseArtifact`, `VanCargoDepositZoneTests.CargoDropInputOutsideVanZoneSpawnsPickupAboveGround`, `VanCargoDepositZoneTests.LargeHeldArtifactObstructsLowerCenterView`, `VanCargoDepositZoneTests.SmallHeldArtifactsOccupyBothSidesOfFirstPersonView`, `VanCargoHoldTests.StoredCargoCanBePickedBackUpFromHold`, `VanCargoHoldTests.SettlementConsumesOnlyCargoLoadedInHold`

Left incomplete:

- `5`, `6`: `EnemyBrain` exists, but the requested dedicated `GhostEnemy` and `DokkaebiEnemy` state sets are not fully represented.
- `7`, `9`, `11`, `12`, `13`, `15`, `16`, `17`, `18`, `20`, `21`, `22`: partial coverage exists, but the full task wording is not yet proven by current tests.

## k-horror-physical-cargo-loop

Marked complete:

- `C4. 밴 cargo 재픽업 구현`
  - Evidence: `VanCargoItem` implements `IInteractable`; `VanCargoHold.TryReleaseToInventory` removes cargo from the hold and restores player-held views.
  - Tests: `VanCargoHoldTests.StoredCargoCanBePickedBackUpFromHold`, `VanCargoHoldTests.StoredCargoPickupFailsWhenHandsAreFull`, `VanCargoHoldTests.RemoveCargoClearsHoldAndFreesSlot`

- `C6. 정산소를 물리 cargo 기준으로 변경`
  - Evidence: `GameLoopController.SettleLoadedCargo` settles through `VanCargoHold.ConsumeSettledCargo`.
  - Tests: `VanCargoHoldTests.SettlementConsumesOnlyCargoLoadedInHold`, `GameLoopControllerTests.BongoHubTerminalSettlesLoadedVanCargoImmediately`, `InternalPlayFlowSmokeTests.MainSceneCompletesPhysicalCargoReturnAndSettlementLoop`

- `C7. 씬 생성기와 귀환 밴 구조 갱신`
  - Evidence: `KHorrorBootstrapSceneBuilder` creates hub and estate return cargo holds, slots, and deposit zones.
  - Tests: `EstateContentIntegrityTests.ReturnBongoHasGCargoDepositZone`, `EstateContentIntegrityTests.BongoHubHasCargoHoldForImmediateSettlement`, `EstateContentIntegrityTests.VanCargoDepositZoneManualDepositLoadsCargoWithoutLeavingEstate`

Additional completion:

- `C5. 일반 드롭과 밴 적재 상태 분리`
  - Evidence: `UnityPlayerController.DropOrDepositCurrentArtifact` keeps hand-held, loose dropped, and van-loaded cargo states mutually exclusive. `VanCargoDepositZone.ContainsActor` provides the zone priority check for the shared drop key.
  - Tests: `VanCargoDepositZoneTests.CargoDropInputInsideVanZoneDepositsInsteadOfDroppingLooseArtifact`, `VanCargoDepositZoneTests.CargoDropInputOutsideVanZoneSpawnsPickupAboveGround`, `VanCargoHoldTests.SettlementConsumesOnlyCargoLoadedInHold`

Left incomplete:

- `C8`: user checkpoint task remains open until the next manual Unity test pass is actually handed off.

## k-horror-lethal-loop-polish

Marked complete:

- `Task 2. 부적/종이문 상호작용 기믹`
  - Evidence: `PaperDoorInteraction` supports intact, torn, and sealed states; talisman-held interaction seals doors and blocks enemy passage.
  - Tests: `PaperDoorInteractionTests.PlayerUsesHeldTalismanToSealDoorTemporarily`, `PaperDoorInteractionTests.SealedDoorBlocksEnemyBrainWithoutTakingDamage`, `PaperDoorInteractionTests.IntactDoorTearsFromEnemyAttackBeforeEnemyCanPass`, `EstateContentIntegrityTests.EstateHasInteractivePaperDoorAndTalismanSamples`

Left incomplete:

- `Task 3`: several Unity mesh integrity checks exist, but `UnityQualityModeMapper` and the explicit low-spec to Unity quality mapping document are not implemented.

## k-horror-threat-ai

No checkbox changes were needed. Existing `H9` through `H14` entries are already marked complete and still match the current code/test evidence.
