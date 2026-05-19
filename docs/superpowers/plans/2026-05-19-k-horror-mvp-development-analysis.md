# K-호러 회수 생존 MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Godot 4 기반 1인칭 싱글플레이 프로토타입으로 `종가 고택` 맵의 회수, 원한 상승, 사운드/위협 강화, 탈출 정산 루프를 구현한다.

**Architecture:** 첫 빌드는 텍스트로 관리하기 쉬운 Godot 4 프로젝트로 구성한다. 게임 규칙은 순수 GDScript 클래스에 두고, 3D 장면은 간단한 `.tscn`과 코드 기반 블록아웃 빌더로 만든다. 회수품, 원한 수치, 할당량, 위협 감독, 오디오 이벤트는 서로 독립된 작은 시스템으로 나눈다.

**Tech Stack:** Godot 4.x, GDScript 2.0, Godot `.tscn` scene files, custom headless GDScript test runner, Git.

---

## Scope

이 계획은 첫 플레이 가능한 세로 조각만 다룬다.

포함:

- Godot 4 프로젝트 골격.
- 헤드리스 테스트 러너.
- 할당량, 회수품, 인벤토리, 원한 수치, 금기, 위협 단계의 순수 로직.
- 1인칭 플레이어 이동과 상호작용 레이.
- 종가 고택 회색 박스 맵.
- 상복 귀신 상태 전환과 혼불/의식 물건 괴이용 이벤트 훅.
- 원한 단계별 오디오 큐 이벤트.
- 탈출 지점 정산과 실패 카운트.

제외:

- 온라인 멀티플레이.
- 정식 3D 아트와 애니메이션.
- 실제 녹음 사운드 에셋.
- 절차 생성 맵.
- 복수 맵과 대형 몬스터 로스터.

## File Structure

- Create: `project.godot`  
  Godot 프로젝트 설정, 메인 씬 지정, 입력 액션 정의.
- Create: `scenes/Main.tscn`  
  게임 진입 씬. `Main.gd`를 붙인 `Node3D`.
- Create: `scenes/player/Player.tscn`  
  `CharacterBody3D`, 카메라, 상호작용 레이를 가진 플레이어 씬.
- Create: `scenes/props/Artifact.tscn`  
  회수품 공통 프롭 씬.
- Create: `scenes/zones/ExtractionZone.tscn`  
  탈출 정산 구역 씬.
- Create: `scripts/game/main.gd`  
  시스템을 조립하고 맵, 플레이어, HUD를 연결하는 런타임 루트.
- Create: `scripts/core/artifact_definition.gd`  
  회수품의 이름, 가치, 무게, 원한 상승량, 금기 태그 정의.
- Create: `scripts/core/inventory.gd`  
  플레이어가 들고 있는 회수품과 총 무게 계산.
- Create: `scripts/core/quota_tracker.gd`  
  할당량, 회수 금액, 빚, 실패 카운트 계산.
- Create: `scripts/core/resentment_tracker.gd`  
  원한 수치와 원한 단계 계산.
- Create: `scripts/core/taboo_rule.gd`  
  금기 위반 이름과 원한 증가량 정의.
- Create: `scripts/core/threat_director.gd`  
  원한 단계에 따라 상복 귀신 상태, 경로 방해, 사운드 큐를 결정.
- Create: `scripts/interactions/interactable.gd`  
  상호작용 가능한 노드의 공통 인터페이스.
- Create: `scripts/props/artifact.gd`  
  회수품 프롭의 상호작용 처리.
- Create: `scripts/zones/extraction_zone.gd`  
  탈출 구역에서 인벤토리를 정산.
- Create: `scripts/player/player_controller.gd`  
  1인칭 이동, 마우스 시점, 무게 기반 감속.
- Create: `scripts/player/interactor.gd`  
  카메라 전방 레이캐스트로 회수품/구역 상호작용.
- Create: `scripts/maps/jongga_estate_builder.gd`  
  종가 고택 회색 박스 맵, 구역, 스폰 포인트 생성.
- Create: `scripts/audio/audio_director.gd`  
  원한 단계와 이벤트 이름에 따른 오디오 큐 재생 및 디버그 로그.
- Create: `scripts/ui/hud.gd`  
  할당량, 회수 금액, 무게, 원한 단계, 상호작용 문구 표시.
- Create: `tests/run_tests.gd`  
  Godot 헤드리스 테스트 러너.
- Create: `tests/test_assertions.gd`  
  테스트 assertion 헬퍼.
- Create: `tests/core/test_quota_tracker.gd`
- Create: `tests/core/test_resentment_tracker.gd`
- Create: `tests/core/test_inventory.gd`
- Create: `tests/core/test_threat_director.gd`

## Task 1: Godot Project And Test Harness

**Files:**

- Create: `project.godot`
- Create: `scenes/Main.tscn`
- Create: `scripts/game/main.gd`
- Create: `tests/run_tests.gd`
- Create: `tests/test_assertions.gd`
- Create: `tests/core/test_quota_tracker.gd`

- [ ] **Step 1: Write the first failing smoke test**

Create `tests/test_assertions.gd`:

```gdscript
extends RefCounted
class_name TestAssertions

var failures: Array[String] = []

func assert_equal(actual: Variant, expected: Variant, message: String) -> void:
	if actual != expected:
		failures.append("%s | expected=%s actual=%s" % [message, str(expected), str(actual)])

func assert_true(value: bool, message: String) -> void:
	if not value:
		failures.append(message)
```

Create `tests/core/test_quota_tracker.gd`:

```gdscript
extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const QuotaTracker = preload("res://scripts/core/quota_tracker.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	var quota := QuotaTracker.new(1000)
	t.assert_equal(quota.required_value, 1000, "quota stores required value")
	t.assert_equal(quota.recovered_value, 0, "quota starts with no recovered value")
	return t.failures
```

- [ ] **Step 2: Create the test runner**

Create `tests/run_tests.gd`:

```gdscript
extends SceneTree

const TESTS := [
	"res://tests/core/test_quota_tracker.gd",
]

func _initialize() -> void:
	var failures: Array[String] = []
	for path in TESTS:
		var test_script: Script = load(path)
		var test_instance = test_script.new()
		var result: Array[String] = test_instance.run()
		for failure in result:
			failures.append("%s: %s" % [path, failure])

	if failures.is_empty():
		print("PASS: %d test files" % TESTS.size())
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		print("FAIL: %d failure(s)" % failures.size())
		quit(1)
```

- [ ] **Step 3: Run test to verify it fails**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: FAIL because `res://scripts/core/quota_tracker.gd` does not exist.

- [ ] **Step 4: Add minimal project and quota class**

Create `project.godot`:

```ini
; Engine configuration file.

config_version=5

[application]
config/name="K Horror Retrieval Prototype"
run/main_scene="res://scenes/Main.tscn"
config/features=PackedStringArray("4.3")

[input]
move_forward={
"deadzone": 0.5,
"events": [Object(InputEventKey,"physical_keycode":87)]
}
move_back={
"deadzone": 0.5,
"events": [Object(InputEventKey,"physical_keycode":83)]
}
move_left={
"deadzone": 0.5,
"events": [Object(InputEventKey,"physical_keycode":65)]
}
move_right={
"deadzone": 0.5,
"events": [Object(InputEventKey,"physical_keycode":68)]
}
interact={
"deadzone": 0.5,
"events": [Object(InputEventKey,"physical_keycode":69)]
}
```

Create `scenes/Main.tscn`:

```ini
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/game/main.gd" id="1_main"]

[node name="Main" type="Node3D"]
script = ExtResource("1_main")
```

Create `scripts/game/main.gd`:

```gdscript
extends Node3D

func _ready() -> void:
	print("K Horror Retrieval Prototype booted")
```

Create `scripts/core/quota_tracker.gd`:

```gdscript
extends RefCounted
class_name QuotaTracker

var required_value: int
var recovered_value: int = 0
var debt: int = 0
var failed_quota_checks: int = 0

func _init(starting_required_value: int = 1000) -> void:
	required_value = starting_required_value

func add_recovered_value(value: int) -> void:
	recovered_value += max(value, 0)

func is_quota_met() -> bool:
	return recovered_value >= required_value

func close_quota_check() -> bool:
	if is_quota_met():
		return true
	var shortfall := required_value - recovered_value
	debt += shortfall
	failed_quota_checks += 1
	return false

func is_contract_ended() -> bool:
	return failed_quota_checks >= 3
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 1 test files`

- [ ] **Step 6: Commit**

```bash
git add project.godot scenes/Main.tscn scripts/game/main.gd scripts/core/quota_tracker.gd tests/run_tests.gd tests/test_assertions.gd tests/core/test_quota_tracker.gd
git commit -m "feat: scaffold Godot prototype and tests"
```

## Task 2: Quota And Artifact Data Model

**Files:**

- Create: `scripts/core/artifact_definition.gd`
- Modify: `scripts/core/quota_tracker.gd`
- Modify: `tests/run_tests.gd`
- Modify: `tests/core/test_quota_tracker.gd`
- Create: `tests/core/test_inventory.gd`

- [ ] **Step 1: Extend failing quota tests**

Replace `tests/core/test_quota_tracker.gd` with:

```gdscript
extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const QuotaTracker = preload("res://scripts/core/quota_tracker.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_quota_success(t)
	test_quota_failure_adds_debt(t)
	test_contract_ends_after_three_failures(t)
	return t.failures

func test_quota_success(t: TestAssertions) -> void:
	var quota := QuotaTracker.new(1000)
	quota.add_recovered_value(1200)
	t.assert_true(quota.close_quota_check(), "quota check succeeds when recovered value is enough")
	t.assert_equal(quota.debt, 0, "successful quota check does not add debt")

func test_quota_failure_adds_debt(t: TestAssertions) -> void:
	var quota := QuotaTracker.new(1000)
	quota.add_recovered_value(350)
	t.assert_true(not quota.close_quota_check(), "quota check fails when value is short")
	t.assert_equal(quota.debt, 650, "shortfall becomes debt")
	t.assert_equal(quota.failed_quota_checks, 1, "failed quota check increments count")

func test_contract_ends_after_three_failures(t: TestAssertions) -> void:
	var quota := QuotaTracker.new(1000)
	quota.close_quota_check()
	quota.close_quota_check()
	quota.close_quota_check()
	t.assert_true(quota.is_contract_ended(), "three failed quota checks ends prototype contract")
```

- [ ] **Step 2: Add failing artifact definition test**

Create `tests/core/test_inventory.gd`:

```gdscript
extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	var bowl := ArtifactDefinition.new("놋 제기", 120, 1.5, 2, ["ancestor_item"])
	t.assert_equal(bowl.display_name, "놋 제기", "artifact stores display name")
	t.assert_equal(bowl.value, 120, "artifact stores value")
	t.assert_equal(bowl.weight, 1.5, "artifact stores weight")
	t.assert_equal(bowl.resentment_gain, 2, "artifact stores resentment gain")
	t.assert_true(bowl.has_tag("ancestor_item"), "artifact stores taboo tag")
	return t.failures
```

Modify `tests/run_tests.gd`:

```gdscript
extends SceneTree

const TESTS := [
	"res://tests/core/test_quota_tracker.gd",
	"res://tests/core/test_inventory.gd",
]

func _initialize() -> void:
	var failures: Array[String] = []
	for path in TESTS:
		var test_script: Script = load(path)
		var test_instance = test_script.new()
		var result: Array[String] = test_instance.run()
		for failure in result:
			failures.append("%s: %s" % [path, failure])

	if failures.is_empty():
		print("PASS: %d test files" % TESTS.size())
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		print("FAIL: %d failure(s)" % failures.size())
		quit(1)
```

- [ ] **Step 3: Run tests to verify artifact test fails**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: FAIL because `artifact_definition.gd` does not exist.

- [ ] **Step 4: Implement artifact definition**

Create `scripts/core/artifact_definition.gd`:

```gdscript
extends RefCounted
class_name ArtifactDefinition

var display_name: String
var value: int
var weight: float
var resentment_gain: int
var tags: Array[String]

func _init(
	p_display_name: String = "",
	p_value: int = 0,
	p_weight: float = 0.0,
	p_resentment_gain: int = 0,
	p_tags: Array[String] = []
) -> void:
	display_name = p_display_name
	value = max(p_value, 0)
	weight = max(p_weight, 0.0)
	resentment_gain = max(p_resentment_gain, 0)
	tags = p_tags.duplicate()

func has_tag(tag: String) -> bool:
	return tags.has(tag)
```

- [ ] **Step 5: Run tests to verify they pass**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 2 test files`

- [ ] **Step 6: Commit**

```bash
git add scripts/core/artifact_definition.gd scripts/core/quota_tracker.gd tests/run_tests.gd tests/core/test_quota_tracker.gd tests/core/test_inventory.gd
git commit -m "feat: add quota and artifact data model"
```

## Task 3: Inventory And Resentment Logic

**Files:**

- Create: `scripts/core/inventory.gd`
- Create: `scripts/core/resentment_tracker.gd`
- Create: `scripts/core/taboo_rule.gd`
- Modify: `tests/run_tests.gd`
- Modify: `tests/core/test_inventory.gd`
- Create: `tests/core/test_resentment_tracker.gd`

- [ ] **Step 1: Write failing inventory tests**

Replace `tests/core/test_inventory.gd` with:

```gdscript
extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ArtifactDefinition = preload("res://scripts/core/artifact_definition.gd")
const Inventory = preload("res://scripts/core/inventory.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_artifact_definition(t)
	test_inventory_adds_items_and_weight(t)
	test_inventory_rejects_overweight_item(t)
	return t.failures

func test_artifact_definition(t: TestAssertions) -> void:
	var bowl := ArtifactDefinition.new("놋 제기", 120, 1.5, 2, ["ancestor_item"])
	t.assert_equal(bowl.display_name, "놋 제기", "artifact stores display name")
	t.assert_true(bowl.has_tag("ancestor_item"), "artifact stores taboo tag")

func test_inventory_adds_items_and_weight(t: TestAssertions) -> void:
	var inv := Inventory.new(5.0)
	var bowl := ArtifactDefinition.new("놋 제기", 120, 1.5, 2, [])
	t.assert_true(inv.try_add(bowl), "inventory accepts item under weight limit")
	t.assert_equal(inv.total_value(), 120, "inventory sums value")
	t.assert_equal(inv.total_resentment_gain(), 2, "inventory sums resentment gain")
	t.assert_equal(inv.total_weight(), 1.5, "inventory sums weight")

func test_inventory_rejects_overweight_item(t: TestAssertions) -> void:
	var inv := Inventory.new(2.0)
	var chest := ArtifactDefinition.new("나전칠기 함", 700, 3.0, 4, [])
	t.assert_true(not inv.try_add(chest), "inventory rejects item over weight limit")
	t.assert_equal(inv.total_value(), 0, "rejected item does not add value")
```

- [ ] **Step 2: Write failing resentment tests**

Create `tests/core/test_resentment_tracker.gd`:

```gdscript
extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ResentmentTracker = preload("res://scripts/core/resentment_tracker.gd")
const TabooRule = preload("res://scripts/core/taboo_rule.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_stage_thresholds(t)
	test_taboo_rule_adds_resentment(t)
	return t.failures

func test_stage_thresholds(t: TestAssertions) -> void:
	var tracker := ResentmentTracker.new()
	t.assert_equal(tracker.stage(), 0, "resentment starts dormant")
	tracker.add_resentment(1, "값싼 물건 회수")
	t.assert_equal(tracker.stage(), 1, "stage 1 starts at resentment 1")
	tracker.add_resentment(4, "사당 물건 회수")
	t.assert_equal(tracker.stage(), 3, "stage 3 starts at resentment 5")
	tracker.add_resentment(7, "금기 연속 위반")
	t.assert_equal(tracker.stage(), 5, "stage caps at 5")

func test_taboo_rule_adds_resentment(t: TestAssertions) -> void:
	var tracker := ResentmentTracker.new()
	var rule := TabooRule.new("문턱 밟기", 2)
	rule.apply_to(tracker)
	t.assert_equal(tracker.current_value, 2, "taboo rule adds resentment")
```

Modify `tests/run_tests.gd`:

```gdscript
extends SceneTree

const TESTS := [
	"res://tests/core/test_quota_tracker.gd",
	"res://tests/core/test_inventory.gd",
	"res://tests/core/test_resentment_tracker.gd",
]

func _initialize() -> void:
	var failures: Array[String] = []
	for path in TESTS:
		var test_script: Script = load(path)
		var test_instance = test_script.new()
		var result: Array[String] = test_instance.run()
		for failure in result:
			failures.append("%s: %s" % [path, failure])

	if failures.is_empty():
		print("PASS: %d test files" % TESTS.size())
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		print("FAIL: %d failure(s)" % failures.size())
		quit(1)
```

- [ ] **Step 3: Run tests to verify they fail**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: FAIL because `inventory.gd`, `resentment_tracker.gd`, and `taboo_rule.gd` do not exist.

- [ ] **Step 4: Implement inventory**

Create `scripts/core/inventory.gd`:

```gdscript
extends RefCounted
class_name Inventory

var max_weight: float
var items: Array[ArtifactDefinition] = []

func _init(p_max_weight: float = 10.0) -> void:
	max_weight = max(p_max_weight, 0.0)

func try_add(item: ArtifactDefinition) -> bool:
	if total_weight() + item.weight > max_weight:
		return false
	items.append(item)
	return true

func clear() -> void:
	items.clear()

func total_value() -> int:
	var sum := 0
	for item in items:
		sum += item.value
	return sum

func total_resentment_gain() -> int:
	var sum := 0
	for item in items:
		sum += item.resentment_gain
	return sum

func total_weight() -> float:
	var sum := 0.0
	for item in items:
		sum += item.weight
	return sum
```

- [ ] **Step 5: Implement resentment and taboo**

Create `scripts/core/resentment_tracker.gd`:

```gdscript
extends RefCounted
class_name ResentmentTracker

signal resentment_changed(value: int, stage: int, reason: String)

var current_value: int = 0
var history: Array[String] = []

func add_resentment(amount: int, reason: String) -> void:
	current_value += max(amount, 0)
	history.append(reason)
	resentment_changed.emit(current_value, stage(), reason)

func stage() -> int:
	if current_value <= 0:
		return 0
	if current_value <= 2:
		return 1
	if current_value <= 4:
		return 2
	if current_value <= 7:
		return 3
	if current_value <= 10:
		return 4
	return 5
```

Create `scripts/core/taboo_rule.gd`:

```gdscript
extends RefCounted
class_name TabooRule

var display_name: String
var resentment_gain: int

func _init(p_display_name: String = "", p_resentment_gain: int = 0) -> void:
	display_name = p_display_name
	resentment_gain = max(p_resentment_gain, 0)

func apply_to(tracker: ResentmentTracker) -> void:
	tracker.add_resentment(resentment_gain, display_name)
```

- [ ] **Step 6: Run tests to verify they pass**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 3 test files`

- [ ] **Step 7: Commit**

```bash
git add scripts/core/inventory.gd scripts/core/resentment_tracker.gd scripts/core/taboo_rule.gd tests/run_tests.gd tests/core/test_inventory.gd tests/core/test_resentment_tracker.gd
git commit -m "feat: add inventory and resentment systems"
```

## Task 4: Threat Director And Audio Events

**Files:**

- Create: `scripts/core/threat_director.gd`
- Create: `scripts/audio/audio_director.gd`
- Modify: `tests/run_tests.gd`
- Create: `tests/core/test_threat_director.gd`

- [ ] **Step 1: Write failing threat director test**

Create `tests/core/test_threat_director.gd`:

```gdscript
extends RefCounted

const TestAssertions = preload("res://tests/test_assertions.gd")
const ThreatDirector = preload("res://scripts/core/threat_director.gd")

func run() -> Array[String]:
	var t := TestAssertions.new()
	test_stage_to_threat_state(t)
	test_audio_cues_by_stage(t)
	return t.failures

func test_stage_to_threat_state(t: TestAssertions) -> void:
	var director := ThreatDirector.new()
	t.assert_equal(director.state_for_stage(0), "dormant", "stage 0 is dormant")
	t.assert_equal(director.state_for_stage(2), "visible", "stage 2 shows apparition")
	t.assert_equal(director.state_for_stage(4), "pursuit", "stage 4 starts pursuit")
	t.assert_equal(director.state_for_stage(5), "contested_extraction", "stage 5 contests extraction")

func test_audio_cues_by_stage(t: TestAssertions) -> void:
	var director := ThreatDirector.new()
	t.assert_true(director.audio_cues_for_stage(1).has("distant_floor_creak"), "stage 1 has subtle sound")
	t.assert_true(director.audio_cues_for_stage(3).has("door_slide_false_route"), "stage 3 has route interference cue")
	t.assert_true(director.audio_cues_for_stage(5).has("close_funeral_wail"), "stage 5 has close threat cue")
```

Modify `tests/run_tests.gd`:

```gdscript
extends SceneTree

const TESTS := [
	"res://tests/core/test_quota_tracker.gd",
	"res://tests/core/test_inventory.gd",
	"res://tests/core/test_resentment_tracker.gd",
	"res://tests/core/test_threat_director.gd",
]

func _initialize() -> void:
	var failures: Array[String] = []
	for path in TESTS:
		var test_script: Script = load(path)
		var test_instance = test_script.new()
		var result: Array[String] = test_instance.run()
		for failure in result:
			failures.append("%s: %s" % [path, failure])

	if failures.is_empty():
		print("PASS: %d test files" % TESTS.size())
		quit(0)
	else:
		for failure in failures:
			push_error(failure)
		print("FAIL: %d failure(s)" % failures.size())
		quit(1)
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: FAIL because `threat_director.gd` does not exist.

- [ ] **Step 3: Implement threat director**

Create `scripts/core/threat_director.gd`:

```gdscript
extends RefCounted
class_name ThreatDirector

func state_for_stage(stage: int) -> String:
	match clamp(stage, 0, 5):
		0:
			return "dormant"
		1:
			return "subtle_presence"
		2:
			return "visible"
		3:
			return "route_interference"
		4:
			return "pursuit"
		5:
			return "contested_extraction"
	return "dormant"

func audio_cues_for_stage(stage: int) -> Array[String]:
	match clamp(stage, 0, 5):
		0:
			return ["night_wind", "paper_door_rattle"]
		1:
			return ["distant_floor_creak", "low_cough"]
		2:
			return ["cloth_drag_far", "faint_funeral_wail"]
		3:
			return ["door_slide_false_route", "ritual_bowl_clink"]
		4:
			return ["cloth_drag_near", "behind_breath"]
		5:
			return ["close_funeral_wail", "locked_gate_hit", "false_vehicle_call"]
	return []
```

- [ ] **Step 4: Implement audio director event sink**

Create `scripts/audio/audio_director.gd`:

```gdscript
extends Node
class_name AudioDirector

signal cue_played(cue_name: String)

var played_cues: Array[String] = []

func play_cue(cue_name: String) -> void:
	played_cues.append(cue_name)
	cue_played.emit(cue_name)
	print("AUDIO_CUE:%s" % cue_name)

func play_stage_cues(cues: Array[String]) -> void:
	for cue in cues:
		play_cue(cue)
```

- [ ] **Step 5: Run tests to verify they pass**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 4 test files`

- [ ] **Step 6: Commit**

```bash
git add scripts/core/threat_director.gd scripts/audio/audio_director.gd tests/run_tests.gd tests/core/test_threat_director.gd
git commit -m "feat: add threat and audio event directors"
```

## Task 5: Player, Interaction, And Artifact Pickup

**Files:**

- Create: `scripts/interactions/interactable.gd`
- Create: `scripts/props/artifact.gd`
- Create: `scenes/props/Artifact.tscn`
- Create: `scripts/player/player_controller.gd`
- Create: `scripts/player/interactor.gd`
- Create: `scenes/player/Player.tscn`
- Modify: `scripts/game/main.gd`

- [ ] **Step 1: Create interactable interface**

Create `scripts/interactions/interactable.gd`:

```gdscript
extends Node3D
class_name Interactable

func interaction_label() -> String:
	return "상호작용"

func interact(_actor: Node) -> void:
	pass
```

- [ ] **Step 2: Create artifact runtime script**

Create `scripts/props/artifact.gd`:

```gdscript
extends Interactable
class_name Artifact

signal picked_up(definition: ArtifactDefinition)

@export var display_name: String = "회수품"
@export var value: int = 100
@export var weight: float = 1.0
@export var resentment_gain: int = 1
@export var tags: Array[String] = []

func definition() -> ArtifactDefinition:
	return ArtifactDefinition.new(display_name, value, weight, resentment_gain, tags)

func interaction_label() -> String:
	return "%s 회수" % display_name

func interact(actor: Node) -> void:
	var item := definition()
	if actor.has_method("try_collect_artifact") and actor.try_collect_artifact(item):
		picked_up.emit(item)
		queue_free()
```

- [ ] **Step 3: Create artifact scene**

Create `scenes/props/Artifact.tscn`:

```ini
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://scripts/props/artifact.gd" id="1_artifact"]

[sub_resource type="BoxMesh" id="BoxMesh_artifact"]
size = Vector3(0.45, 0.3, 0.45)

[sub_resource type="BoxShape3D" id="BoxShape_artifact"]
size = Vector3(0.45, 0.3, 0.45)

[node name="Artifact" type="StaticBody3D"]
script = ExtResource("1_artifact")

[node name="MeshInstance3D" type="MeshInstance3D" parent="."]
mesh = SubResource("BoxMesh_artifact")

[node name="CollisionShape3D" type="CollisionShape3D" parent="."]
shape = SubResource("BoxShape_artifact")
```

- [ ] **Step 4: Create player controller**

Create `scripts/player/player_controller.gd`:

```gdscript
extends CharacterBody3D
class_name PlayerController

@export var base_speed: float = 4.5
@export var mouse_sensitivity: float = 0.0025
@export var gravity: float = 9.8

var inventory := Inventory.new(12.0)
var camera: Camera3D

func _ready() -> void:
	camera = $Camera3D
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		rotate_y(-event.relative.x * mouse_sensitivity)
		camera.rotate_x(-event.relative.y * mouse_sensitivity)
		camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-80), deg_to_rad(80))

func _physics_process(delta: float) -> void:
	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	var weight_ratio := clamp(inventory.total_weight() / inventory.max_weight, 0.0, 1.0)
	var speed := lerp(base_speed, base_speed * 0.55, weight_ratio)
	velocity.x = direction.x * speed
	velocity.z = direction.z * speed
	if not is_on_floor():
		velocity.y -= gravity * delta
	move_and_slide()

func try_collect_artifact(item: ArtifactDefinition) -> bool:
	return inventory.try_add(item)
```

- [ ] **Step 5: Create interactor**

Create `scripts/player/interactor.gd`:

```gdscript
extends RayCast3D
class_name PlayerInteractor

@export var actor_path: NodePath
var actor: Node
var current_label: String = ""

func _ready() -> void:
	actor = get_node(actor_path)

func _process(_delta: float) -> void:
	current_label = ""
	if is_colliding():
		var hit := get_collider()
		if hit != null and hit.has_method("interaction_label"):
			current_label = hit.interaction_label()
			if Input.is_action_just_pressed("interact"):
				hit.interact(actor)
```

- [ ] **Step 6: Create player scene**

Create `scenes/player/Player.tscn`:

```ini
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://scripts/player/player_controller.gd" id="1_player"]
[ext_resource type="Script" path="res://scripts/player/interactor.gd" id="2_interactor"]

[sub_resource type="CapsuleShape3D" id="CapsuleShape_player"]
height = 1.8
radius = 0.35

[node name="Player" type="CharacterBody3D"]
script = ExtResource("1_player")

[node name="CollisionShape3D" type="CollisionShape3D" parent="."]
shape = SubResource("CapsuleShape_player")

[node name="Camera3D" type="Camera3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1.55, 0)

[node name="Interactor" type="RayCast3D" parent="Camera3D"]
target_position = Vector3(0, 0, -2.4)
enabled = true
script = ExtResource("2_interactor")
actor_path = NodePath("../..")
```

- [ ] **Step 7: Connect artifact pickup to resentment in main**

Replace `scripts/game/main.gd` with:

```gdscript
extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")

var player: PlayerController
var resentment := ResentmentTracker.new()
var threat_director := ThreatDirector.new()
var audio_director: AudioDirector

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	player = PlayerScene.instantiate()
	add_child(player)
	player.global_position = Vector3(0, 1, 0)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("K Horror Retrieval Prototype booted")

func register_artifact(artifact: Artifact) -> void:
	artifact.picked_up.connect(_on_artifact_picked_up)

func _on_artifact_picked_up(definition: ArtifactDefinition) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(_value: int, stage: int, _reason: String) -> void:
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))
```

- [ ] **Step 8: Run tests**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 4 test files`

- [ ] **Step 9: Commit**

```bash
git add scripts/interactions/interactable.gd scripts/props/artifact.gd scenes/props/Artifact.tscn scripts/player/player_controller.gd scripts/player/interactor.gd scenes/player/Player.tscn scripts/game/main.gd
git commit -m "feat: add player interaction and artifact pickup"
```

## Task 6: Jongga Estate Blockout And Extraction

**Files:**

- Create: `scripts/maps/jongga_estate_builder.gd`
- Create: `scripts/zones/extraction_zone.gd`
- Create: `scenes/zones/ExtractionZone.tscn`
- Modify: `scripts/game/main.gd`

- [ ] **Step 1: Create extraction zone script**

Create `scripts/zones/extraction_zone.gd`:

```gdscript
extends Area3D
class_name ExtractionZone

signal extracted(total_value: int)

func extract_inventory(inventory: Inventory) -> int:
	var value := inventory.total_value()
	inventory.clear()
	extracted.emit(value)
	return value
```

- [ ] **Step 2: Create extraction zone scene**

Create `scenes/zones/ExtractionZone.tscn`:

```ini
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://scripts/zones/extraction_zone.gd" id="1_extract"]

[sub_resource type="BoxShape3D" id="BoxShape_extract"]
size = Vector3(3, 2, 3)

[node name="ExtractionZone" type="Area3D"]
script = ExtResource("1_extract")

[node name="CollisionShape3D" type="CollisionShape3D" parent="."]
shape = SubResource("BoxShape_extract")
```

- [ ] **Step 3: Create code-based Jongga Estate blockout**

Create `scripts/maps/jongga_estate_builder.gd`:

```gdscript
extends Node3D
class_name JonggaEstateBuilder

const ArtifactScene := preload("res://scenes/props/Artifact.tscn")
const ExtractionScene := preload("res://scenes/zones/ExtractionZone.tscn")

func build(main: Node) -> void:
	_create_floor("바깥마당", Vector3(0, 0, -8), Vector3(12, 0.2, 10), Color.DARK_GREEN)
	_create_floor("사랑채", Vector3(-7, 0, -15), Vector3(8, 0.2, 7), Color.DIM_GRAY)
	_create_floor("안채", Vector3(7, 0, -22), Vector3(9, 0.2, 8), Color.SADDLE_BROWN)
	_create_floor("곳간", Vector3(-8, 0, -27), Vector3(6, 0.2, 6), Color.DARK_SLATE_GRAY)
	_create_floor("사당", Vector3(0, 0, -36), Vector3(7, 0.2, 7), Color.MAROON)
	_spawn_artifact(main, "놋 제기", 120, 1.5, 2, Vector3(-5, 0.4, -15), ["ancestor_item"])
	_spawn_artifact(main, "서예 족자", 280, 1.0, 3, Vector3(7, 0.4, -22), ["document_item"])
	_spawn_artifact(main, "사당 방울", 700, 2.0, 5, Vector3(0, 0.4, -36), ["shrine_item"])
	var extraction := ExtractionScene.instantiate()
	add_child(extraction)
	extraction.global_position = Vector3(0, 0.5, 2)

func _create_floor(label: String, pos: Vector3, scale: Vector3, color: Color) -> void:
	var body := StaticBody3D.new()
	body.name = label
	add_child(body)
	body.global_position = pos
	var mesh_instance := MeshInstance3D.new()
	var box_mesh := BoxMesh.new()
	box_mesh.size = scale
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	box_mesh.material = mat
	mesh_instance.mesh = box_mesh
	body.add_child(mesh_instance)
	var collision := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = scale
	collision.shape = shape
	body.add_child(collision)

func _spawn_artifact(main: Node, display_name: String, value: int, weight: float, resentment_gain: int, pos: Vector3, tags: Array[String]) -> void:
	var artifact := ArtifactScene.instantiate()
	add_child(artifact)
	artifact.display_name = display_name
	artifact.value = value
	artifact.weight = weight
	artifact.resentment_gain = resentment_gain
	artifact.tags = tags
	artifact.global_position = pos
	if main.has_method("register_artifact"):
		main.register_artifact(artifact)
```

- [ ] **Step 4: Spawn map and quota in main**

Replace `scripts/game/main.gd` with:

```gdscript
extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilder := preload("res://scripts/maps/jongga_estate_builder.gd")
const QuotaTracker := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")

var player: PlayerController
var quota := QuotaTracker.new(800)
var resentment := ResentmentTracker.new()
var threat_director := ThreatDirector.new()
var audio_director: AudioDirector

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	var map := JonggaEstateBuilder.new()
	add_child(map)
	map.build(self)
	player = PlayerScene.instantiate()
	add_child(player)
	player.global_position = Vector3(0, 1, 4)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("K Horror Retrieval Prototype booted")

func register_artifact(artifact: Artifact) -> void:
	artifact.picked_up.connect(_on_artifact_picked_up)

func _on_artifact_picked_up(definition: ArtifactDefinition) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(value: int, stage: int, reason: String) -> void:
	print("원한:%d 단계:%d 이유:%s" % [value, stage, reason])
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))

func extract_player_inventory() -> void:
	var value := player.inventory.total_value()
	player.inventory.clear()
	quota.add_recovered_value(value)
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])
```

- [ ] **Step 5: Run tests and launch smoke check**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 4 test files`

Run:

```bash
godot --path .
```

Expected: The project opens to a 3D scene with a first-person player, colored blockout zones, and three collectible artifact boxes.

- [ ] **Step 6: Commit**

```bash
git add scripts/maps/jongga_estate_builder.gd scripts/zones/extraction_zone.gd scenes/zones/ExtractionZone.tscn scripts/game/main.gd
git commit -m "feat: add Jongga Estate blockout and extraction"
```

## Task 7: HUD And Playability Pass

**Files:**

- Create: `scripts/ui/hud.gd`
- Modify: `scripts/game/main.gd`

- [ ] **Step 1: Create HUD script**

Create `scripts/ui/hud.gd`:

```gdscript
extends CanvasLayer
class_name HUD

var label: Label

func _ready() -> void:
	label = Label.new()
	label.position = Vector2(20, 20)
	label.add_theme_font_size_override("font_size", 18)
	add_child(label)

func update_status(quota_value: int, quota_required: int, weight: float, max_weight: float, resentment_stage: int, interaction_label: String) -> void:
	label.text = "회수금액 %d/%d\n무게 %.1f/%.1f\n원한 단계 %d\n%s" % [
		quota_value,
		quota_required,
		weight,
		max_weight,
		resentment_stage,
		interaction_label
	]
```

- [ ] **Step 2: Connect HUD in main**

Modify `scripts/game/main.gd` so it includes HUD setup and status refresh:

```gdscript
extends Node3D

const PlayerScene := preload("res://scenes/player/Player.tscn")
const AudioDirectorScript := preload("res://scripts/audio/audio_director.gd")
const JonggaEstateBuilder := preload("res://scripts/maps/jongga_estate_builder.gd")
const QuotaTracker := preload("res://scripts/core/quota_tracker.gd")
const ResentmentTracker := preload("res://scripts/core/resentment_tracker.gd")
const ThreatDirector := preload("res://scripts/core/threat_director.gd")
const HUDScript := preload("res://scripts/ui/hud.gd")

var player: PlayerController
var quota := QuotaTracker.new(800)
var resentment := ResentmentTracker.new()
var threat_director := ThreatDirector.new()
var audio_director: AudioDirector
var hud: HUD

func _ready() -> void:
	audio_director = AudioDirectorScript.new()
	add_child(audio_director)
	var map := JonggaEstateBuilder.new()
	add_child(map)
	map.build(self)
	player = PlayerScene.instantiate()
	add_child(player)
	player.global_position = Vector3(0, 1, 4)
	hud = HUDScript.new()
	add_child(hud)
	resentment.resentment_changed.connect(_on_resentment_changed)
	print("K Horror Retrieval Prototype booted")

func _process(_delta: float) -> void:
	var interaction_label := ""
	var interactor := player.get_node_or_null("Camera3D/Interactor")
	if interactor != null:
		interaction_label = interactor.current_label
	hud.update_status(
		quota.recovered_value,
		quota.required_value,
		player.inventory.total_weight(),
		player.inventory.max_weight,
		resentment.stage(),
		interaction_label
	)

func register_artifact(artifact: Artifact) -> void:
	artifact.picked_up.connect(_on_artifact_picked_up)

func _on_artifact_picked_up(definition: ArtifactDefinition) -> void:
	resentment.add_resentment(definition.resentment_gain, "%s 회수" % definition.display_name)

func _on_resentment_changed(value: int, stage: int, reason: String) -> void:
	print("원한:%d 단계:%d 이유:%s" % [value, stage, reason])
	audio_director.play_stage_cues(threat_director.audio_cues_for_stage(stage))

func extract_player_inventory() -> void:
	var value := player.inventory.total_value()
	player.inventory.clear()
	quota.add_recovered_value(value)
	print("정산:%d / 할당량:%d" % [quota.recovered_value, quota.required_value])
```

- [ ] **Step 3: Run tests and manual smoke check**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 4 test files`

Run:

```bash
godot --path .
```

Expected:

- WASD moves the player.
- Mouse moves the first-person camera.
- Looking at an artifact shows Korean interaction text.
- Pressing `E` picks up an artifact.
- HUD updates weight and 원한 단계.
- Console prints audio cue names when 원한 단계 changes.

- [ ] **Step 4: Commit**

```bash
git add scripts/ui/hud.gd scripts/game/main.gd
git commit -m "feat: add prototype HUD"
```

## Task 8: Prototype Readme And Final Verification

**Files:**

- Modify: `README.md`

- [ ] **Step 1: Update README with run instructions**

Replace `README.md` with:

````markdown
# K_horror_game

K-호러 테마의 회수 생존 게임 프로토타입입니다. `Lethal Company`류의 할당량/회수/탈출 루프를 한국적 장소, 문화재급 회수품, 금기, 귀신/요괴/민속 괴이로 재해석합니다.

## MVP

- 엔진: Godot 4.x
- 플레이 방식: 1인칭 싱글플레이
- 첫 맵: 종가 고택
- 핵심 루프: 회수품 탐색 → 원한 수치 상승 → 사운드/위협 강화 → 탈출 정산

## 실행

```bash
godot --path .
```

## 테스트

```bash
godot --headless --path . --script res://tests/run_tests.gd
```
````

- [ ] **Step 2: Run full verification**

Run:

```bash
godot --headless --path . --script res://tests/run_tests.gd
```

Expected: `PASS: 4 test files`

Run:

```bash
git diff --check
```

Expected: no output and exit code 0.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: add prototype run instructions"
```

## Self-Review Checklist

- Spec coverage:
  - 회수/할당량 루프: Tasks 2, 6, 8.
  - 원한 수치와 금기 기반 위험 상승: Task 3.
  - 상복 귀신 상태 전환과 혼불/의식 물건 괴이의 이벤트 기반 토대: Task 4.
  - 사운드 신호: Tasks 4, 7.
  - 종가 고택 첫 맵 규모와 구역감: Task 6.
  - 1인칭 싱글플레이 MVP: Tasks 5, 6, 7.
- Placeholder scan:
  - 모든 코드 작성 단계에는 생성하거나 교체할 파일 내용이 포함되어 있다.
  - 실제 구현 단계에서 빈칸 지시문이 생기면 해당 커밋 전에 구체 코드와 명령으로 교체한다.
- Type consistency:
  - `ArtifactDefinition`, `Inventory`, `QuotaTracker`, `ResentmentTracker`, `TabooRule`, `ThreatDirector` 이름을 전 태스크에서 동일하게 사용한다.
  - `try_collect_artifact`, `register_artifact`, `extract_player_inventory` 메서드명을 전 태스크에서 동일하게 사용한다.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-19-k-horror-mvp-development-analysis.md`. Two execution options:

1. Subagent-Driven (recommended) - I dispatch a fresh subagent per task, review between tasks, fast iteration

2. Inline Execution - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
