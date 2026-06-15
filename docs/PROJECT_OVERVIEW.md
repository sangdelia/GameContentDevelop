# STARGRAVE SURVIVOR 프로젝트 상세 설명

## 개요

`STARGRAVE SURVIVOR`는 SF 우주 기지 분위기의 생존형 액션 게임입니다. 플레이어는 제한된 아레나 안에서 적 웨이브를 상대하고, 경험치를 모아 레벨업 특성을 선택하며, 최종적으로 보스 아레나에서 대형 보스를 처치하는 구조입니다.

이 프로젝트는 PC 테스트 플레이를 우선 지원하면서 Meta Quest 2 VR 모드로 확장할 수 있도록 입력, 카메라, UI, 플레이어 루트 구성을 분리해 두었습니다.

## 플레이 루프

1. 런타임 UI에서 PC 테스트 또는 VR 모드를 선택합니다.
2. 플레이어가 아레나에서 이동하고 조준하며 적을 처치합니다.
3. 적 처치 시 경험치 오브가 생성되고, 플레이어에게 끌려와 경험치를 제공합니다.
4. 레벨업 시 3개의 특성 선택지가 표시됩니다.
5. 선택한 특성은 즉시 플레이어 능력치, 탄환 속성, 상태이상, 오라 효과 등에 반영됩니다.
6. 요구 처치 수를 채우면 보스 포털이 열립니다.
7. 포털 진입 후 보스 아레나로 이동하고, 보스 처치 시 클리어 UI가 표시됩니다.
8. 플레이어 사망 시 게임오버 UI가 표시되며 `RETRY`로 현재 씬을 다시 시작할 수 있습니다.

## 조작

PC 테스트 모드는 다음 조작을 사용합니다.

- 이동: `WASD`
- 시점/조준: 마우스
- 사격: 마우스 왼쪽 버튼
- 시작 메뉴 빠른 시작: `Enter`
- 커서 해제: `Escape`

VR 모드는 `PlatformModeManager`와 `SimpleQuest2VrRig`가 런타임 XR Origin, 카메라, 컨트롤러 기준점을 구성합니다. 현재 구조에서는 PC 더미 플레이어 루트를 유지하면서 VR 조준 소스만 교체할 수 있도록 되어 있습니다.

## 주요 시스템

### 플레이어

- `PlayerDummyMove`: PC 이동, 마우스 시점 회전, 충돌 기반 슬라이딩 이동을 담당합니다.
- `PlayerShootTest`: 레이캐스트 기반 사격, 총구 이펙트, 탄환 색상, 무기 반동, 직접 피해 적용을 담당합니다.
- `PlayerHealth`: 체력, 방어력, 체력 재생, 처치 회복, 보호막, 사망 이벤트를 관리합니다.
- `PlayerLevel`: 경험치, 레벨, 레벨업 선택 요청 이벤트를 관리합니다.
- `SimpleQuest2VrRig`: VR 테스트용 머리/손 비주얼과 조준 기준점을 구성합니다.

### 적

- `EnemySpawner`: 일반 적, 원거리 적, 비행 원거리 적을 생성합니다.
- `EnemyMoveToPlayer`: 근접 적 추적과 접촉 공격을 담당합니다.
- `EnemyRangedAttack`: 일정 거리 유지 후 투사체를 발사하는 원거리 적입니다.
- `EnemyFlyingRangedAttack`: 공중 높이를 유지하며 공격하는 비행 적입니다.
- `EnemyHealth`: 피해 처리, 사망 이벤트, 경험치 드롭을 담당합니다.
- `EnemyVisual`: 런타임 모델, 공격 예고, 발사 반동, 히트 반응 등 적 비주얼을 구성합니다.

### 특성 및 상태이상

- `PlayerTraitController`: 레벨업 선택지 추첨, 특성 레벨 저장, 특성 적용을 담당합니다.
- `TraitData`: ScriptableObject 기반 특성 정의입니다.
- `PlayerCombatStats`: 피해량과 공격 속도 계수를 계산합니다.
- `StatusEffectController`: 적에게 적용되는 화상과 둔화 상태이상을 관리합니다.
- `PlayerAuraController`: 주변 적 둔화 오라를 주기적으로 갱신합니다.

특성 시스템의 자세한 내용은 [특성 시스템 상세 설명](TRAIT_SYSTEM.md)을 참고합니다.

### 진행도와 보스전

- `GameProgressManager`: 처치 수, 포털 개방, 보스 아레나 생성, 플레이어 보스 아레나 이동, 보스 생성 이벤트를 담당합니다.
- `BossArenaBuilder`: 보스 전용 임시 아레나, 바닥, 벽, 장식, 엄폐물을 런타임에 생성합니다.
- `TempBossController`: 보스 모델 로드, 보스 이동, 근접 공격, 레이저, 화염 방사, 사망 처리, 클리어 UI 호출을 담당합니다.

보스 아레나는 현재 런타임 생성 방식이며, `GameProgressManager`의 `bossArenaSize`, `bossArenaCenter`, `bossSpawnPosition`, `bossPlayerSpawnPosition` 값으로 위치와 크기를 조절합니다.

### UI

- `StargraveRuntimeUI`: 시작 화면, HUD, 레벨업 선택 화면, 게임오버/클리어 화면을 런타임에 생성합니다.
- Unity UI `Canvas`, `Button`, `Text`, `Image`, `EventSystem`, `InputSystemUIInputModule`을 코드에서 보장합니다.
- 게임오버/클리어 화면에서는 플레이어 이동/사격을 끄고, 커서를 표시해 `RETRY`와 `QUIT` 버튼을 마우스로 선택할 수 있게 합니다.

## 아트와 리소스

- `Assets/Resources/Models/KenneySpace`: 우주 기지 배경용 모듈러 모델입니다.
- `Assets/Resources/Models/Boss`: 보스 모델 프리팹 리소스입니다.
- `Assets/Sci-Fi ToiletMech`: 외부 보스 모델 원본 리소스입니다.
- `Assets/ThirdParty`: 외부 아트/애니메이션 라이브러리 리소스입니다.

## 런타임 생성 구조

프로젝트는 많은 오브젝트를 프리팹 배치보다 코드 생성에 의존합니다. 이 방식은 테스트 씬을 빠르게 반복하기 좋지만, 다음 사항을 주의해야 합니다.

- UI는 `RuntimeInitializeOnLoadMethod`로 자동 생성됩니다.
- 보스 아레나는 포털 개방 전 준비되고, 보스전 진입 시 활성화됩니다.
- 보스와 일반 적 비주얼은 런타임에 모델과 머티리얼을 조립합니다.
- 런타임 생성 오브젝트는 씬에 직접 보이지 않으므로, 디버깅 시 Hierarchy에서 Play Mode 상태를 확인해야 합니다.

## 빌드/검증

코드 변경 후 기본 검증 명령은 다음과 같습니다.

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Unity 에디터에서는 Play Mode로 다음 흐름을 확인합니다.

- 시작 화면 버튼 클릭
- 일반 적 처치와 경험치 획득
- 레벨업 특성 선택
- 탄환 색상 및 상태이상 적용
- 보스 아레나 진입
- 보스 사망 후 클리어 UI
- 플레이어 사망 후 `RETRY` 재시작

## 개발 시 주의점

- 플레이어 입력 로직과 특성 로직은 분리합니다. PC/VR 차이는 입력 소스에서만 처리하고, 특성 효과는 공통으로 유지합니다.
- 탄환 속성 특성은 마지막으로 선택한 속성 하나만 활성화합니다.
- 일반 적 이동 코드는 단순하고 즉각적인 이동을 유지합니다. 특성 둔화는 이동 속도 배율만 반영합니다.
- 보스 모델은 애니메이션 루트와 런타임 위치 보정이 충돌하기 쉬우므로, 바닥 스냅과 모델 앵커 처리를 함께 확인해야 합니다.
