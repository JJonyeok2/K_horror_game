# Implementation Plan

- [x] Task 1. 봉고차 내부 즉시 정산 및 HUD/모니터 연동
  - `BongoRunStateMachineTests`에 허브 단말기 즉시 정산 실패 테스트를 추가한다.
  - `BongoRunStateMachine`에서 `SettlementOffice` 자동 이동 대신 허브 즉시 정산을 수행한다.
  - `GameLoopController`가 물리 `VanCargoHold` 정산 금액을 상태 머신과 쿼터에 반영하고 HUD 피드백을 표시하게 한다.
  - `BongoMonitorDisplay`와 단말기 문구가 적재 가치, 적재 개수, 쿼터를 실시간 표시하게 한다.
  - 씬 생성기에 필요한 참조를 추가하고 `KHorror_Main.unity`를 재생성한다.
  - EditMode 테스트와 `git diff --check`를 실행한 뒤 커밋/푸시한다.
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

- [x] Task 2. 부적/종이문 상호작용 기믹
  - `TalismanSealInteractable` 또는 동등 컴포넌트의 실패 테스트를 작성한다.
  - 부적 사용 시 문 봉인 상태와 몬스터 진입 차단 상태를 일정 시간 유지한다.
  - `PaperDoorInteractable`의 1회성 찢김 상태와 시야 확보 표시를 구현한다.
  - 고택 씬에 최소 부적, 봉인 문, 종이문 샘플을 배치한다.
  - EditMode 테스트와 `git diff --check`를 실행한 뒤 커밋/푸시한다.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] Task 3. Godot 메시 검증 및 저사양 모드 이관 베이스
  - Godot `test_estate_mesh_integrity.gd` 검사 항목을 Unity EditMode 테스트 목록으로 옮긴다.
  - 대문 하단, 대문-담장 접합부, 안채/신당 진입로, 장식 박힘 검사를 추가한다.
  - `UnityQualityModeMapper` 에디터 베이스 코드와 설정 매핑 문서를 작성한다.
  - 실패 메시지에 오브젝트명과 월드 좌표를 포함한다.
  - 전체 EditMode 테스트와 `git diff --check`를 실행한 뒤 커밋/푸시한다.
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
