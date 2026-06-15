# 특성 시스템 상세 설명

## 목적

특성 시스템은 레벨업 때 플레이어에게 3개의 선택지를 보여주고, 선택한 능력을 즉시 전투에 반영하는 성장 시스템입니다. PC와 VR 모드가 같은 특성 규칙을 공유하도록 설계되어 있으며, 입력 장치 차이는 `PlayerShootTest`와 플레이어 이동 스크립트에서만 처리합니다.

## 핵심 흐름

1. `PlayerLevel`이 레벨업 조건을 만족하면 `LevelUpChoicesRequested` 이벤트를 발생시킵니다.
2. `StargraveRuntimeUI`가 이벤트를 받아 게임 시간을 멈추고 특성 선택 패널을 표시합니다.
3. UI는 `PlayerTraitController.RollChoices()`를 호출해 선택지 3개를 받습니다.
4. 플레이어가 버튼 또는 숫자키 `1 / 2 / 3`으로 특성을 선택합니다.
5. `PlayerTraitController.ApplyTrait()`가 특성 레벨을 올리고 실제 효과를 적용합니다.
6. UI가 닫히고 게임 시간이 다시 흐릅니다.

## 주요 클래스

### `TraitData`

특성 하나를 정의하는 ScriptableObject입니다.

주요 필드:

- `traitId`: 특성을 구분하는 고유 ID입니다.
- `displayName`: UI 표시 이름입니다.
- `description`: 기본 설명입니다.
- `category`: 탄환 변경, 능력치, 오라, 특수 효과 분류입니다.
- `rarity`: Common, Rare, Epic, Legendary 등급입니다.
- `effectKind`: 실제 적용 로직을 결정하는 효과 종류입니다.
- `maxLevel`: 최대 레벨입니다.
- `levels`: 레벨별 수치 목록입니다.

`TraitLevelData`는 한 레벨의 수치를 담습니다.

- `value`: 주 효과 수치입니다.
- `secondaryValue`: 보조 수치입니다.
- `duration`: 지속 시간입니다.
- `probability`: 확률입니다.
- `radius`: 범위입니다.
- `maxStacks`: 최대 중첩 수입니다.

### `PlayerTraitController`

특성 선택, 레벨 저장, 효과 적용의 중심 클래스입니다.

주요 역할:

- 기본 특성 카탈로그 생성
- 선택지 랜덤 추첨
- 특성 레벨 저장
- 탄환 속성 상태 저장
- 피해량/공격 속도/이동 속도/체력/방어/회복/보호막 적용
- 직접 명중 시 화상, 둔화, 즉사 처리

특성 레벨은 `Dictionary<string, int>` 형태로 저장됩니다. 키는 `TraitData.traitId`입니다.

### `PlayerCombatStats`

무기 피해량과 공격 속도 계수를 관리합니다.

- `GetFinalDamage(baseDamage)`: 기본 피해량에 피해량 배율을 적용합니다.
- `GetFinalShotsPerSecond(baseShotsPerSecond)`: 기본 연사 속도에 공격 속도 배율을 적용합니다.
- 최소 발사 간격을 보호해 공격 속도가 비정상적으로 높아지는 것을 막습니다.

### `PlayerShootTest`

사격과 특성 연결 지점입니다.

사격 처리 순서:

1. 조준 레이를 만듭니다.
2. 가까운 적 보정 또는 일반 Raycast로 타격 대상을 찾습니다.
3. 현재 탄환 속성에 맞는 레이 색상을 표시합니다.
4. `EnemyHealth`에 직접 피해를 적용합니다.
5. `PlayerTraitController.HandleDirectEnemyHit()`로 탄환 속성 및 특수 효과를 적용합니다.

탄환 색상:

- 일반탄: 노란색
- 화염탄: 빨간색
- 빙결탄: 파란색/하늘색

### `StatusEffectController`

적에게 필요한 순간 동적으로 붙는 상태이상 컨트롤러입니다.

현재 사용 효과:

- `Burn`: 화상 피해를 일정 간격으로 적용합니다.
- `Slow`: 이동 속도 배율을 낮춥니다.

상태이상은 `effectId` 기준으로 갱신됩니다. 같은 효과를 다시 적용하면 지속 시간이 갱신되거나 중첩 수가 증가합니다.

### `PlayerAuraController`

플레이어 주변 적에게 둔화 효과를 주는 오라를 관리합니다.

- `ConfigureSlowAura(radius, normalSlow, bossSlow, interval)`로 활성화됩니다.
- 일반 적과 보스의 둔화 배율을 다르게 적용할 수 있습니다.
- `Physics.OverlapSphereNonAlloc`을 사용해 주기적으로 주변 적을 검사합니다.

## 탄환 속성 규칙

탄환 속성은 동시에 여러 개가 켜지지 않습니다.

- 화염탄을 선택하면 현재 탄환 속성이 `Fire`가 됩니다.
- 빙결탄을 선택하면 현재 탄환 속성이 `Ice`가 됩니다.
- 마지막으로 선택한 탄환 속성만 실제 사격에 적용됩니다.

이 규칙은 `PlayerTraitController.CurrentProjectileElement`가 관리합니다.

## 현재 특성 목록

### 화염탄

- ID: `fire_projectile`
- 분류: `ProjectileModifier`
- 최대 레벨: 3
- 효과: 탄환 속성을 화염으로 교체합니다.
- 명중 시 적에게 화상 상태이상을 부여합니다.
- 화상은 직접 피해량 일부를 틱 피해로 변환해 일정 시간 동안 적용합니다.
- 레벨이 오르면 화상 피해 비율과 최대 중첩이 강화됩니다.

### 빙결탄

- ID: `ice_projectile`
- 분류: `ProjectileModifier`
- 최대 레벨: 3
- 효과: 탄환 속성을 얼음으로 교체합니다.
- 명중 시 적에게 둔화 상태이상을 부여합니다.
- 레벨이 오르면 둔화 비율과 지속 시간이 강화됩니다.

### 피해 강화

- ID: `damage`
- 분류: `StatBuff`
- 최대 레벨: 5
- 효과: 무기 직접 피해량을 증가시킵니다.
- `PlayerCombatStats`의 피해 배율을 갱신합니다.

### 공격 속도 강화

- ID: `attack_speed`
- 분류: `StatBuff`
- 최대 레벨: 5
- 효과: 초당 발사 횟수를 증가시킵니다.
- 최소 발사 간격 제한을 넘지 않는 선에서 적용됩니다.

### 이동 속도 강화

- ID: `move_speed`
- 분류: `StatBuff`
- 최대 레벨: 5
- 효과: PC 이동 속도와 VR 이동 속도를 증가시킵니다.
- `PlayerDummyMove`와 `SimpleQuest2VrRig`에 모두 반영됩니다.

### 경험치 자석

- ID: `magnet`
- 분류: `StatBuff`
- 최대 레벨: 5
- 효과: 경험치 오브 흡입 범위를 증가시킵니다.
- `ExpOrb.AddGlobalAttractBonus()`로 전역 흡입 보너스를 누적합니다.

### 최대 체력 증가

- ID: `max_health`
- 분류: `StatBuff`
- 최대 레벨: 4
- 효과: 최대 체력을 증가시킵니다.
- 체력 UI에도 즉시 반영됩니다.

### 방어력

- ID: `armor`
- 분류: `StatBuff`
- 최대 레벨: 4
- 효과: 받는 피해를 고정 수치만큼 감소시킵니다.

### 체력 재생

- ID: `health_regen`
- 분류: `StatBuff`
- 최대 레벨: 5
- 효과: 초당 체력 재생량을 추가합니다.

### 처치 회복

- ID: `heal_on_kill`
- 분류: `StatBuff`
- 최대 레벨: 5
- 효과: 일반 적 처치 시 체력을 회복합니다.

### 보호막

- ID: `shield`
- 분류: `StatBuff`
- 최대 레벨: 4
- 효과: 일정 시간마다 1회 피해를 막는 보호막을 강화합니다.

### 중력 덫

- ID: `slow_aura`
- 분류: `Aura`
- 최대 레벨: 1
- 효과: 주변 적의 이동 속도를 낮춥니다.
- 일반 적과 보스에게 다른 둔화 비율을 적용합니다.

### 처형 탄두

- ID: `instant_kill`
- 분류: `Special`
- 최대 레벨: 5
- 효과: 일반 적 명중 시 일정 확률로 즉시 처치합니다.
- 보스에게는 적용하지 않습니다.

## 상태이상 세부 동작

### 화상

화상은 `StatusEffectController.ApplyBurn()`으로 적용됩니다.

- 동일 `effectId`의 화상은 기존 효과를 갱신합니다.
- 중첩 수는 `maxStacks`를 넘지 않습니다.
- 틱마다 `DamageInfo`를 만들어 `EnemyHealth.TakeDamage()`를 호출합니다.
- 화상 상태일 때 적에게 화염 파티클과 링 비주얼이 표시됩니다.

### 둔화

둔화는 `StatusEffectController.ApplySlow()`로 적용됩니다.

- 이동 속도 배율은 `MoveSpeedMultiplier`로 노출됩니다.
- 여러 둔화가 있을 경우 가장 강한 둔화, 즉 가장 낮은 배율을 사용합니다.
- 근접 적, 원거리 적, 비행 적은 이 배율을 자신의 이동 속도에 곱해 사용합니다.
- 둔화 상태일 때 적에게 서리 파티클과 파란 링 비주얼이 표시됩니다.

## UI 표시

특성 선택 UI는 `StargraveRuntimeUI`가 생성합니다.

선택 버튼에는 다음 정보가 표시됩니다.

- 번호
- 특성 이름
- 다음 레벨에서 적용될 효과 설명
- 현재 선택 후 레벨 / 최대 레벨

특성 선택 중에는 `Time.timeScale = 0`으로 게임을 멈추고, 선택 후 다시 `1`로 복구합니다.

## 새 특성 추가 방법

1. `TraitEffectKind`에 새 효과 종류를 추가합니다.
2. `PlayerTraitController.ApplyTrait()`에 적용 로직을 추가합니다.
3. 직접 명중 효과라면 `HandleDirectEnemyHit()`에 분기 처리를 추가합니다.
4. 적에게 지속 효과가 필요하면 `StatusEffectController`에 효과 타입과 갱신 로직을 추가합니다.
5. 새 특성 데이터를 `TraitData` 에셋으로 만들거나 `EnsureDefaultCatalog()`의 런타임 카탈로그에 추가합니다.
6. UI 설명이 필요하면 `GetChoiceDescription()`, `GetLocalizedDisplayName()`, `GetLocalizedDescription()`을 갱신합니다.

## 설계 원칙

- 특성 규칙은 PC와 VR에서 동일하게 유지합니다.
- 입력 스크립트 안에 특성 효과를 직접 늘리지 않습니다.
- 탄환 속성은 한 번에 하나만 활성화합니다.
- 상태이상은 적에게 필요한 순간 `StatusEffectController`를 붙여 처리합니다.
- 보스에게 즉사처럼 밸런스를 깨는 효과는 적용하지 않습니다.
- UI 문구는 가능하면 한글 이름과 한글 설명을 유지합니다.
