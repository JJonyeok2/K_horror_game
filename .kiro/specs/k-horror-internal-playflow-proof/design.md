# Design Document

## 개요

이 스펙은 새 게임 시스템을 크게 늘리기보다, 이미 구현된 핵심 시스템을 실제 플레이 순서로 묶어 검증하는 내부 QA 레일을 추가한다. 자동화 대상은 Unity EditMode 테스트와 Editor 스크린샷 캡처다.

## 구조

### InternalPlayFlowSmokeTests

`Unity_Migration/KHorrorUnity/Assets/KHorrorGame/Migration/CoreLoop/Tests/EditMode/InternalPlayFlowSmokeTests.cs`

- `KHorror_Main.unity`를 연다.
- `GameLoopController`, `UnityPlayerController`, `VanCargoDepositZone`, `VanCargoHold`, `RuntimeThreatSpawner`, `ThreatAtmosphereCue`를 실제 씬에서 찾는다.
- private `Awake`/`Start`가 EditMode에서 자동 실행되지 않는 경우 reflection으로 초기화한다.
- 봉고차 허브 출발, 고택 도착, 화물 적재, 재픽업, 재적재, 허브 복귀, 즉시 정산을 한 테스트에서 검증한다.
- 신당급 아이템 획득 후 grace 종료와 위협 actor 활성화를 검증한다.

### KHorrorScreenshotCapture

`Unity_Migration/KHorrorUnity/Assets/KHorrorGame/Editor/KHorrorScreenshotCapture.cs`

- 기존 `CaptureCargoRepickupProof`, `CaptureTerminalUiProof`, `CaptureThreatAtmosphereProof` 패턴을 따른다.
- 새 `CaptureInternalPlayFlowProof` 메뉴/정적 메서드를 추가한다.
- 증빙 이미지는 `Artifacts/Screenshots/internal-playflow-proof.png`로 저장한다.
- 한 화면에 봉고차 단말기/화물/손 오브젝트/위협 cue를 배치한 내부 QA 이미지로 만든다.

### Testable screenshot check

Editor 코드가 테스트 어셈블리에서 직접 호출 가능한 상태라면 테스트에서 캡처 메서드를 호출한다. 어셈블리 참조 문제로 직접 호출이 어렵다면 Unity batchmode `-executeMethod KHorrorGame.EditorTools.KHorrorScreenshotCapture.CaptureInternalPlayFlowProof`를 별도 검증 명령으로 실행한다.

## 검증

- RED: `InternalPlayFlowSmokeTests`를 먼저 추가하고, 새 스크린샷 메서드나 통합 흐름 보조가 없어서 실패하는 것을 확인한다.
- GREEN: 필요한 최소 구현을 추가한다.
- Screenshot: Unity batchmode로 `CaptureInternalPlayFlowProof`를 실행하고 PNG 존재와 크기를 확인한다.
- Regression: 전체 EditMode 테스트를 실행한다.
- Hygiene: `git diff --check`, 테스트 XML 삭제, 커밋, 원격 푸시.

## 비범위

- Godot `.gd`, `.tscn` 파일 수정은 하지 않는다.
- 실제 최종 아트 퀄리티 작업은 이 스펙의 목적이 아니다.
- PlayMode 빌드 자동화는 이후 별도 Task로 확장한다.
