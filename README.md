# K_horror_game

K-호러 테마의 회수 생존 게임 프로토타입입니다. `Lethal Company`류의 할당량/회수/탈출 루프를 한국적 장소, 문화재급 회수품, 금기, 귀신/요괴/민속 괴이로 재해석합니다.

## MVP

- 엔진: Godot 4.x
- 플레이 방식: 1인칭 싱글플레이
- 첫 맵: 종가 고택
- 핵심 루프: 회수품 탐색 -> 원한 수치 상승 -> 사운드/위협 강화 -> 탈출 정산

## 실행

```bash
godot --path .
```

## 조작

- 이동: `WASD`
- 점프: `Space`
- 회수/상호작용: `E`
- 소지품: 2칸 제한

## 테스트

```bash
godot --headless --path . --script res://tests/run_tests.gd
```
