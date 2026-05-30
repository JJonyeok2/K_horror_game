# Requirements Document

## 개요

이 스펙은 Unity 이관판 `K_horror_game`의 핵심 플레이 흐름을 사용자의 수동 테스트 없이 내부 자동 검증과 스크린샷 증빙으로 확인하기 위한 작업이다.

현재 프로젝트에는 봉고차 허브, 종가 고택, 물리 화물 적재, 재픽업, 봉고차 단말기, 부적/종이문, 위협 AI, 사운드 오클루전이 개별 단위로 구현되어 있다. 이제 이 기능들이 실제 플레이 순서에서 끊기지 않는지 한 번에 검증하는 레일이 필요하다.

## Requirement 1: 전체 회수 루프 자동 검증

**User Story:** 개발자로서, 봉고차에서 출발해 종가에서 아이템을 훔치고, 봉고차에 적재하고, 다시 허브로 돌아와 정산하는 핵심 루프가 한 테스트 안에서 증명되기를 원한다.

### Acceptance Criteria

1. WHEN 내부 스모크 테스트가 실행되면 THEN `KHorror_Main.unity`의 실제 생성 씬을 열고 `GameLoopController`를 초기화해야 한다.
2. WHEN 봉고차 단말기 동작을 시뮬레이션하면 THEN 상태는 `BongoHub`에서 이동 상태를 거쳐 `JonggaEstate`로 전환되어야 한다.
3. WHEN 플레이어가 고택에서 아이템을 소지하고 `VanCargoDepositZone`에 적재하면 THEN 플레이어 손은 비고 `EstateReturnBongo`의 `VanCargoHold`에는 물리 `VanCargoItem`이 남아야 한다.
4. WHEN 플레이어가 적재된 화물을 다시 집으면 THEN `VanCargoHold`의 화물 수와 가치가 감소하고 플레이어의 1인칭 손 오브젝트가 복구되어야 한다.
5. WHEN 다시 적재한 뒤 봉고차로 복귀하면 THEN 고택 봉고의 화물은 허브 봉고의 `VanCargoHold`로 이전되어야 한다.
6. WHEN 허브 단말기로 정산하면 THEN 쿼터 회수 금액이 증가하고 허브 화물은 제거되어야 한다.

## Requirement 2: 위협 루프 자동 검증

**User Story:** 플레이어로서, 신당 최고 가치 아이템을 훔쳤을 때 위협 단계와 몬스터 출현이 실제 루프 안에서 체감 가능한 상태로 이어지기를 원한다.

### Acceptance Criteria

1. WHEN 신당 태그가 붙은 아이템을 획득하면 THEN 원한 단계는 최고 단계로 상승해야 한다.
2. WHEN 신당 훔침 직후라면 THEN 짧은 grace window 동안 즉시 공격 스폰이 억제되어야 한다.
3. WHEN grace window가 끝나고 `RuntimeThreatSpawner`가 평가되면 THEN 고택 내부 귀신 actor 중 하나 이상이 활성화되어야 한다.
4. WHEN 활성화된 귀신이 사운드 오클루전 컴포넌트를 가진다면 THEN AudioSource, AudioLowPassFilter, 생성 cue clip이 함께 연결되어야 한다.
5. WHEN 최고 위협 분위기 큐가 실행되면 THEN 안개/등불/HUD 피드백이 자동 테스트에서 확인 가능해야 한다.

## Requirement 3: 스크린샷 증빙

**User Story:** 개발자로서, 자동 검증 결과를 말로만 설명하지 않고 실제 화면 증빙으로 확인하고 싶다.

### Acceptance Criteria

1. WHEN 스크린샷 캡처 명령이 실행되면 THEN `Artifacts/Screenshots/internal-playflow-proof.png`가 생성되어야 한다.
2. WHEN 증빙 이미지가 열리면 THEN 봉고차 단말기, 적재 화물, 손에 든 화물, 고택/신당 위협 cue가 한 화면 또는 분할 구성으로 드러나야 한다.
3. WHEN 테스트가 실행되면 THEN 스크린샷 파일의 존재와 최소 파일 크기를 확인해야 한다.
4. IF 캡처 중 Unity가 실패하면 THEN 테스트는 조용히 통과하지 않고 실패해야 한다.

## Requirement 4: 작업 단위 검증과 푸시

**User Story:** 개발자로서, 각 작업 단위가 Windows와 Mac에서 이어받기 쉬운 상태로 원격 브랜치에 올라가기를 원한다.

### Acceptance Criteria

1. WHEN 각 Task 구현이 끝나면 THEN 관련 EditMode 테스트와 전체 EditMode 테스트를 실행해야 한다.
2. WHEN 테스트가 통과하면 THEN `git diff --check`를 실행해야 한다.
3. WHEN 검증이 끝나면 THEN 논리적 커밋을 만들고 `origin/unity-conversion`으로 푸시해야 한다.
4. WHEN Unity 테스트 산출 XML이 생기면 THEN 커밋에 포함하지 않아야 한다.
