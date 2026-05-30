# Implementation Plan

- [x] Task 1. 내부 전체 플레이 루프 스모크 테스트와 증빙 스크린샷
  - `KHorror_Main.unity` 실제 씬을 열어 봉고차 허브 -> 고택 -> 화물 적재 -> 화물 재픽업 -> 재적재 -> 허브 복귀 -> 즉시 정산 흐름을 EditMode 테스트로 검증한다.
  - 신당급 아이템 획득 후 최고 위협 단계, grace window, 귀신 actor 활성화, 사운드 오클루전 연결을 같은 작업 단위에서 검증한다.
  - `KHorrorScreenshotCapture.CaptureInternalPlayFlowProof`를 추가해 `Artifacts/Screenshots/internal-playflow-proof.png`를 생성한다.
  - 관련 RED 테스트를 먼저 작성해 실패를 확인한 뒤 구현한다.
  - 관련 테스트, 전체 EditMode 테스트, `git diff --check`를 실행한다.
  - 커밋하고 `origin/unity-conversion`으로 푸시한다.
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4_

- [x] Task 2. 자동 증빙 리포트 생성
  - 내부 스모크 테스트 결과, 주요 상태 값, 생성 스크린샷 경로를 요약하는 `Artifacts/Reports/internal-playflow-proof.md`를 생성한다.
  - 리포트에는 현재 브랜치, 커밋 SHA, 테스트 총합, 스크린샷 파일 크기를 포함한다.
  - batchmode에서 재실행해도 이전 리포트를 안전하게 갱신한다.
  - 커밋하고 `origin/unity-conversion`으로 푸시한다.
  - _Requirements: 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4_

- [ ] Task 3. 다음 미완료 스펙 상태 정리
  - `k-horror-unity-next-milestone`, `k-horror-threat-ai`, `k-horror-physical-cargo-loop`, `k-horror-lethal-loop-polish`의 체크 상태와 실제 코드 증거를 대조한다.
  - 이미 다른 스펙에서 완료된 중복 task는 근거 커밋/테스트를 남기고 체크 상태를 정리한다.
  - 아직 실제로 미완료인 task는 다음 Kiro 작업 단위 후보로 남긴다.
  - 커밋하고 `origin/unity-conversion`으로 푸시한다.
  - _Requirements: 4.1, 4.2, 4.3, 4.4_
