# Implementation Plan

- [ ] C1. 물리 적재 도메인 테스트 작성
  - `VanCargoHold`가 cargo item 개수와 총 가치를 계산하는 EditMode 테스트를 추가한다.
  - 밴 밖 물건은 정산 가치에 포함되지 않는 테스트를 추가한다.
  - 정산 완료 시 cargo item 목록이 비워지는 테스트를 추가한다.
  - 이 단계에서는 테스트가 실패해야 정상이다.
  - _Requirements: 1.5, 2.1, 2.2, 2.5, 4.2, 4.3_

- [ ] C2. `VanCargoHold`와 `VanCargoItem` 구현
  - `VanCargoHold` 컴포넌트를 추가해 슬롯, 예비 위치, cargo 목록, 총 가치 계산을 담당하게 한다.
  - `VanCargoItem` 컴포넌트를 추가해 원본 회수품 데이터, 가치, 크기, 재픽업 가능 상태를 보존하게 한다.
  - 정산 완료 시 cargo item을 안전하게 제거하는 API를 추가한다.
  - C1 테스트를 통과시킨 뒤 커밋하고 `origin/unity-conversion`에 푸시한다.
  - _Requirements: 1.1, 1.3, 1.4, 1.6, 2.1, 2.2_

- [ ] C3. `VanCargoDepositZone`을 물리 적재 방식으로 교체
  - 기존 `ExtractPlayerInventory()` 직접 호출 흐름을 제거한다.
  - `G` 입력 시 손에 든 물건을 `VanCargoHold`에 `VanCargoItem`으로 생성한다.
  - 생성 성공 후에만 플레이어 인벤토리와 손 시각화를 비운다.
  - 손에 든 물건이 없거나 hold가 없을 때 실패 피드백을 표시한다.
  - EditMode 테스트를 실행하고, Unity를 열어 사용자가 밴 적재를 직접 확인할 수 있게 한다.
  - _Requirements: 1.1, 1.2, 1.6, 4.1, 4.2_

- [ ] C4. 밴 cargo 재픽업 구현
  - `VanCargoItem`이 `E` 상호작용으로 다시 플레이어 인벤토리에 들어가게 한다.
  - 재픽업 성공 시 cargo hold 목록에서 제거한다.
  - 손이 가득 차 있으면 재픽업을 막고 중앙 하단 프롬프트에 이유를 표시한다.
  - 재픽업 후 회수품 가치, 크기, 양손 여부, 위협 상승 정보가 유지되는 테스트를 추가한다.
  - _Requirements: 1.3, 1.4, 3.4, 3.5, 3.6_

- [ ] C5. 일반 드롭과 밴 적재 상태 분리
  - 일반 바닥에서 `G`를 누르면 `DroppedArtifact`로 내려놓는 흐름을 복구 또는 보강한다.
  - 밴 내부 적재 구역에서의 `G`와 일반 드롭 `G`가 충돌하지 않도록 우선순위를 정한다.
  - 드롭 아이템이 바닥 아래로 생성되지 않도록 위치 보정 테스트를 추가한다.
  - 밴 밖 드롭 아이템은 정산 합계에 포함되지 않음을 테스트한다.
  - _Requirements: 1.5, 3.3, 3.4, 3.6, 4.3_

- [ ] C6. 정산소를 물리 cargo 기준으로 변경
  - 정산 UI와 정산 계산이 `PendingRecoveredValue` 대신 `VanCargoHold` 합계를 사용하게 한다.
  - 정산 성공 시 cargo item을 제거하고 할당량 또는 정산 금액에 반영한다.
  - 손에 든 물건은 밴에 내려놓지 않았다면 정산 대상에서 제외한다.
  - 회수품 없음, 정산 성공, hold 누락 실패 피드백을 검증한다.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.1_

- [ ] C7. 씬 생성기와 귀환 밴 구조 갱신
  - `KHorrorBootstrapSceneBuilder`가 귀환 밴 내부에 `VanCargoHold`, 적재 구역, 슬롯 anchor를 생성하게 한다.
  - 슬롯은 플레이어 시야를 막지 않되 밴 내부에 보이도록 배치한다.
  - 씬 무결성 테스트로 cargo hold와 슬롯이 누락되지 않았는지 검증한다.
  - 씬을 재생성하고 Unity YAML 변경을 확인한다.
  - _Requirements: 1.1, 1.6, 4.4, 4.5_

- [ ] C8. 사용자 테스트 체크포인트
  - EditMode 테스트와 `git diff --check`를 실행한다.
  - 변경 사항을 커밋하고 `origin/unity-conversion`에 푸시한다.
  - Unity를 실행해 사용자가 직접 다음 루프를 확인하게 한다: 종가 이동, 물건 줍기, 밴 복귀, `G` 적재, 재진입, cargo 재픽업, 정산.
  - 사용자 피드백을 받은 뒤 다음 Spec 또는 다음 작업 단위로 넘어간다.
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
