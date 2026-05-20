# K_horror_game

한국형 호러 회수 생존 게임 프로토타입입니다. `Lethal Company`식 흐름을 기준으로, 봉고차 내부 허브에서 맵을 선택하고, 고택에서 물건을 회수한 뒤, 봉고차로 복귀해 정산소 맵에서 정산하는 구조를 구현 중입니다.

## 현재 개발 브랜치

현재 플레이 가능한 MVP 작업 브랜치는 `k-horror-mvp2`입니다.

```bash
git switch k-horror-mvp2
```

## 필요 환경

- Git
- Godot 4.6.x
- macOS 기준으로 테스트 중
- Windows/Linux도 Godot 4.6.x에서 열 수 있지만, 현재 안내는 macOS 중심입니다.

Godot 다운로드:

https://godotengine.org/download/macos/

## 처음 설치하기

원하는 작업 폴더에서 저장소를 복제합니다.

```bash
git clone https://github.com/JJonyeok2/K_horror_game.git
cd K_horror_game
git switch k-horror-mvp2
```

외부 PBR 머티리얼은 `assets/external/ambientcg/materials` 아래에 포함되어 있습니다. 별도 그래픽 패키지를 다시 받을 필요는 없습니다.

## Godot 앱으로 실행하기

1. Godot 4.6.x를 실행합니다.
2. Project Manager에서 `Import`를 누릅니다.
3. 이 저장소의 `project.godot` 파일을 선택합니다.
4. 프로젝트가 열리면 우측 상단 실행 버튼을 누릅니다.

메인 씬은 이미 `project.godot`에 지정되어 있습니다.

```ini
run/main_scene="res://scenes/Main.tscn"
```

## 터미널에서 실행하기

Godot CLI가 `godot` 명령으로 잡혀 있으면 다음처럼 실행합니다.

```bash
godot --path .
```

macOS에서 `godot` 명령이 없으면 Godot 앱 내부 실행 파일을 직접 사용할 수 있습니다.

```bash
/Applications/Godot.app/Contents/MacOS/Godot --path .
```

자주 쓸 경우 `~/.zshrc`에 alias를 추가할 수 있습니다.

```bash
echo 'alias godot="/Applications/Godot.app/Contents/MacOS/Godot"' >> ~/.zshrc
source ~/.zshrc
godot --path .
```

## 현재 게임 흐름

현재 구현된 기본 루프는 다음과 같습니다.

1. 게임 시작
2. 봉고차 내부 허브에서 시작
3. 봉고차 내부 단말기에서 `종가 고택` 선택
4. 봉고차 이동 상태 진입
5. 고택 도착 후 문 열림
6. 고택을 돌아다니며 아이템 파밍
7. 아이템을 봉고차 적재 구역에 싣기
8. 봉고차 복귀 버튼으로 봉고차 내부 허브 복귀
9. 봉고차 내부에서 `정산소` 선택
10. 정산소 맵으로 이동
11. 정산소 카운터에서 미정산 물품 정산

정산 금액은 플레이어 HUD에 직접 표시되지 않고, 봉고차 내부 모니터와 정산소 흐름에서 확인하는 방식입니다.

## 조작법

- 이동: `W`, `A`, `S`, `D`
- 시점: 마우스
- 상호작용: `E`
- 달리기: `Shift`
- 점프: `Space`
- 들고 있는 아이템 버리기: `G`
- 마우스 캡처 해제: `Esc`

## 저사양 모드

현재 프로젝트는 기본적으로 저사양 모드가 켜져 있습니다.

```ini
[k_horror]
low_spec_mode=true
```

저사양 모드에서는 다음이 적용됩니다.

- 외부 PBR 텍스처 로딩 비활성화
- 안개 비활성화
- 런타임 조명 감소
- Godot 기본 머티리얼 중심 렌더링

고품질 머티리얼 경로를 확인하려면 `project.godot`에서 값을 바꿉니다.

```ini
[k_horror]
low_spec_mode=false
```

## 테스트 실행

Godot CLI가 `godot`으로 잡혀 있으면 코어 테스트를 실행할 수 있습니다.

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

주요 씬 테스트는 아래처럼 개별 실행합니다.

```bash
godot --headless --path . --script res://tests/scene/test_bongo_monitor_quota.gd
godot --headless --path . --script res://tests/scene/test_playable_scene.gd
godot --headless --path . --script res://tests/scene/test_threat_health_loop.gd
godot --headless --path . --script res://tests/scene/test_sprint_threat_and_folklore.gd
godot --headless --path . --script res://tests/scene/test_estate_density_and_interactions.gd
godot --headless --path . --script res://tests/scene/test_low_spec_mode.gd
godot --headless --path . --script res://tests/scene/test_external_materials.gd
godot --headless --path . --script res://tests/scene/test_estate_route_structure.gd
```

Godot CLI가 alias로만 등록되어 있지 않다면 macOS에서 다음처럼 직접 실행합니다.

```bash
/Applications/Godot.app/Contents/MacOS/Godot --headless --path . --script res://tests/run_tests.gd
```

## 문제 해결

### Godot이 바로 꺼지는 경우

터미널에서 실행해 오류 로그를 확인합니다.

```bash
godot --path .
```

또는 macOS 앱 경로를 직접 사용합니다.

```bash
/Applications/Godot.app/Contents/MacOS/Godot --path .
```

### 화면이 무겁거나 렉이 걸리는 경우

`project.godot`에서 저사양 모드가 켜져 있는지 확인합니다.

```ini
[k_horror]
low_spec_mode=true
```

### 상호작용 문구가 안 보이는 경우

상호작용 대상에 가까이 간 뒤 화면 중앙으로 오브젝트를 바라보고 `E`를 누릅니다. 봉고차 내부 단말기, 복귀 버튼, 정산소 카운터는 모두 이 방식으로 작동합니다.

### 정산이 안 되는 경우

정산은 봉고차 내부에서 바로 끝나지 않습니다.

1. 고택에서 아이템을 회수합니다.
2. 봉고차 적재 구역에 싣습니다.
3. 봉고차 복귀 버튼으로 봉고차 허브에 돌아갑니다.
4. 봉고차 내부에서 정산소를 선택합니다.
5. 정산소 카운터에서 `E`로 정산합니다.
