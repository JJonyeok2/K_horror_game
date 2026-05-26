# Design Document

## 개요

이번 마일스톤은 세 개의 독립 작업으로 나눈다. 첫 번째는 봉고 내부에서 화물 가치와 쿼터를 확인하고 즉시 정산하는 리썰 컴퍼니식 핵심 루프다. 두 번째는 부적과 종이문을 `IInteractable` 기반 컴포넌트로 추가해 한국형 호러의 독자적 상호작용을 만든다. 세 번째는 Godot 시절의 메시 무결성 검사를 Unity EditMode 테스트와 에디터 보조 코드로 이관한다.

## Task 1 설계: 봉고차 즉시 정산

기존 `BongoRunStateMachine`은 `PendingRecoveredValue`가 있으면 봉고 허브 단말기에서 `SettlementOffice`로 이동하도록 설계되어 있다. 이를 바꿔 봉고 허브에서 바로 `SettleStoredCargo()`가 호출되게 한다. 물리 화물은 `VanCargoHold`가 이미 들고 있으므로, `GameLoopController`가 현재 활성 봉고의 `VanCargoHold`를 참조하고 정산 시 `ConsumeSettledCargo()` 결과를 상태 머신에 전달한다.

핵심 변경점은 다음과 같다.

- `BongoRunStateMachine`: 허브에서 pending cargo가 있으면 정산을 수행하고, 단말기 문구를 "Settle loaded cargo"로 바꾼다.
- `GameLoopController`: 정산 성공 시 `ShowFeedback("+000 settled")` 형태의 HUD 피드백을 띄운다.
- `BongoMonitorDisplay`: `PendingRecoveredValue`, 화물 개수, 쿼터 진행도를 항상 표시한다.
- `KHorrorBootstrapSceneBuilder`: 봉고 내부 단말기가 정산 겸 출발 단말기로 동작하도록 씬 참조를 유지한다.

`SettlementOffice` 씬과 기존 위험 요소는 당장 삭제하지 않는다. 단, 메인 루프에서 단말기로 자동 이동하지 않게 해 이후 별도 이벤트/특수 계약 공간으로 재활용할 수 있게 둔다.

## Task 2 설계: 부적과 종이문

부적은 회수 아이템과 다르게 소비형 유틸리티로 다룬다. `TalismanItem`은 플레이어 인벤토리의 일반 화물과 충돌하지 않도록 별도 상태 또는 태그 기반으로 판정한다. `SealTargetDoor`는 부적을 받으면 `EnemyDoorBlocker` 상태를 일정 시간 활성화하고 HUD 피드백을 표시한다.

종이문은 `PaperDoorInteractable` 컴포넌트로 구현한다. 첫 상호작용 시 찢김 상태를 저장하고, 렌더러/콜라이더를 시야 확보용 상태로 바꾼다. 물리 파괴 시뮬레이션 대신 MVP 단계에서는 찢긴 구멍 표시 오브젝트와 콜라이더 축소로 표현한다.

## Task 3 설계: Unity 메시 검증 이관

Godot의 메시 무결성 테스트는 Unity EditMode 테스트로 옮긴다. `EstateContentIntegrityTests`의 기존 패턴을 확장하고, 반복되는 Raycast/Bounds 검사는 별도 테스트 헬퍼로 분리한다. 저사양 모드 매핑은 `UnityQualityModeMapper` 에디터 유틸리티로 시작해, Godot 설정값을 읽는 가이드와 Unity URP 품질 옵션 매핑표를 제공한다.

검증 범위는 대문/담장/바닥/장식의 플레이어 이탈 방지와 Z-fighting 위험 지점 위주로 제한한다. 메시 자동 수정은 이 Spec의 범위가 아니며, 실패 위치를 명확히 알려 수동/생성기 수정을 쉽게 만드는 데 집중한다.

## 테스트 전략

- Task 1은 `BongoRunStateMachineTests`, `GameLoopControllerTests`, `VanCargoHoldTests`, `EstateContentIntegrityTests`를 통해 정산 루프, HUD 문구, 씬 참조를 검증한다.
- Task 2는 부적 봉인 시간, 몬스터 통과 차단, 종이문 1회성 찢김 상태를 EditMode 테스트로 검증한다.
- Task 3은 씬 기반 EditMode 테스트로 대문/담장/장식/바닥 유격을 검증한다.

각 Task 완료 시 전체 EditMode 테스트와 `git diff --check`를 실행하고 `origin/unity-conversion`에 푸시한다.
