# Design Document

## 개요

이번 마일스톤은 이미 구축된 시스템을 플레이어가 손으로 만질 수 있는 루프로 닫는 작업이다. 우선 `VanCargoHold`에 적재된 `VanCargoItem`을 다시 `IInteractable` 대상으로 만들어 화물 재픽업 루프를 완성한다. 그 뒤 봉고 단말기 UI, 종이문/부적 기믹, AI 압박 연출을 추가한다.

구현은 기존 Unity 구조를 유지한다. 핵심 런타임 코드는 `Assets/KHorrorGame/Migration/CoreLoop/Scripts` 아래에 두고, 회귀 검증은 `Assets/KHorrorGame/Migration/CoreLoop/Tests/EditMode`에 추가한다. Godot 파일은 참고용으로만 유지한다.

## Task 1 설계: 적재 화물 재픽업

`VanCargoItem`은 현재 적재된 물건의 정의와 `VanCargoHold` 참조를 보유한다. 여기에 `IInteractable`을 구현해 `PlayerInteractor`의 기존 카메라 레이캐스트 흐름을 그대로 탄다.

상호작용 흐름은 다음과 같다.

1. 플레이어가 적재 화물을 바라본다.
2. `PlayerInteractor`가 `VanCargoItem`을 `IInteractable`로 감지한다.
3. `VanCargoItem.CanInteract(actor)`가 플레이어 손 슬롯과 인벤토리 수용 가능 여부를 확인한다.
4. `VanCargoItem.Interact(actor)`가 `CargoHold.TryReleaseToInventory(this, actor)`를 호출한다.
5. 성공 시 `VanCargoHold`는 내부 목록에서 해당 화물을 제거하고, `UnityPlayerController.TryCollectArtifact(definition)`을 통해 손 시야 가림을 다시 생성한다.
6. 실패 시 화물은 적재 상태로 유지되고 HUD 피드백만 표시된다.

`VanCargoHold`에는 재픽업을 위한 명시적 API를 둔다.

- `bool CanReleaseToInventory(VanCargoItem cargoItem, UnityPlayerController actor)`
- `bool TryReleaseToInventory(VanCargoItem cargoItem, UnityPlayerController actor, out string failureReason)`

이렇게 하면 `VanCargoItem`은 상호작용 진입점만 담당하고, 적재 목록과 총액 갱신은 `VanCargoHold`가 계속 소유한다.

## Task 2 설계: 봉고 단말기와 프롬프트 UI

봉고 단말기는 `GameLoopController.GetTerminalStatusText()`의 데이터를 그대로 쓰되, 표시 계층을 분리한다. `VanTerminalController`는 Canvas 기반 패널을 소유하고, `BongoMonitorDisplay` 또는 기존 단말기 오브젝트에서 갱신 텍스트를 받아 typewriter 효과를 적용한다.

중앙 하단 프롬프트는 `IInteractable.InteractionLabel`을 계속 사용한다. 다만 각 상호작용 컴포넌트가 더 명확한 한국어 라벨을 제공하도록 한다.

- `VanCargoItem`: `[E] 화물 다시 들기 - {이름}`
- `ArtifactPickup`: `[E] 물건 줍기 - {이름}`
- `BongoTerminal`: `[E] 단말기 조작`
- `PaperDoorInteraction`: `[E] 종이문 찢기` 또는 `[E] 부적 붙이기`

## Task 3 설계: 종이문과 부적

`PaperDoorInteraction`은 종이문의 상태를 `Intact`, `Torn`, `Sealed`로 관리한다. 종이문은 찢기 전에는 시야와 AI 인지를 막는 얇은 차단물처럼 동작하고, 찢긴 뒤에는 시야 단서와 AI 인지가 열리는 상태로 바뀐다.

`TalismanSeal`은 플레이어가 가진 부적 정의 또는 태그를 사용해 문에 부착된다. 봉인 상태에서는 `EnemyBrain`이 해당 문을 통과하거나 공격하지 못하도록 간단한 차단 판정을 제공한다. NavMesh 우회는 이후 단계로 미루고, 이번 Task에서는 문 통과/파괴 금지와 피드백을 먼저 보장한다.

## Task 4 설계: 사운드 오클루전과 위협 연출

`ThreatAudioOcclusion`은 플레이어와 AI 사이를 Raycast하고 벽/문이 있으면 `AudioLowPassFilter`와 볼륨을 조절한다. 오디오 에셋이 없어도 테스트 가능한 구조를 위해 현재 볼륨/필터 컷오프 값을 외부에서 읽을 수 있게 한다.

`ThreatAtmosphereCue`는 최고 위협 단계 도달 시 주변 Light 컴포넌트 깜빡임과 안개/노출 계열 값 변경을 트리거한다. 실제 그래픽 품질은 후속 패스에서 높이고, 이번에는 이벤트 연결과 플레이어가 알아차릴 수 있는 확실한 피드백을 우선한다.

## 테스트 전략

Task 1은 `VanCargoHoldTests`와 `VanCargoDepositZoneTests`에 재픽업 테스트를 추가한다. 적재 후 재픽업 시 개수/가치 감소, 손 시야 가림 생성, 실패 시 상태 유지가 핵심이다.

Task 2는 UI 문자열과 typewriter 진행 상태를 EditMode에서 검증한다. 실제 화면 미감은 Unity 수동 테스트로 확인한다.

Task 3은 종이문 상태 전이, 부적 봉인 시간, AI 차단 판정을 EditMode에서 검증한다.

Task 4는 오디오 오클루전 수치와 최고 위협 단계 환경 큐 트리거를 EditMode에서 검증한다.

각 Task 완료 후 전체 EditMode 테스트와 `git diff --check`를 실행하고 원격 브랜치 `origin/unity-conversion`에 푸시한다.
