# STARGRAVE SURVIVOR

Unity 기반의 SF 생존 액션 프로젝트입니다. PC 테스트 조작을 기본으로 두고, Meta Quest 2 VR 모드 확장을 고려한 구조로 구성되어 있습니다.

## 핵심 플레이

- 플레이어는 아레나에서 몰려오는 적을 처치하고 경험치 오브를 획득합니다.
- 레벨업 시 3개의 특성 중 하나를 선택해 빌드를 강화합니다.
- 일정 처치 수를 달성하면 보스 포털이 열리고, 보스 아레나로 이동합니다.
- 보스전은 근접 공격, 레이저, 화염 방사 패턴을 피하며 클리어하는 흐름입니다.

## 주요 문서

- [프로젝트 상세 설명](docs/PROJECT_OVERVIEW.md)
- [특성 시스템 상세 설명](docs/TRAIT_SYSTEM.md)
- [게임 기획 문서](docs/GAME_SPEC.md)
- [진행/성장 시스템 문서](docs/PROGRESSION_SPEC.md)
- [VR 조작 명세](docs/VR_CONTROL_SPEC.md)

## 주요 코드 위치

- 플레이어 조작/사격: `Assets/Scripts/Player`
- 적 AI/체력/비주얼: `Assets/Scripts/Enemy`
- 특성/상태이상: `Assets/Scripts/Traits`
- 진행도/보스전: `Assets/Scripts/Progress`
- 런타임 UI: `Assets/Scripts/UI`

## 현재 상태

프로젝트는 런타임 생성 UI와 런타임 생성 보스 아레나를 사용합니다. Unity 에디터에서 씬을 실행하면 PC 테스트 모드로 플레이할 수 있으며, 런타임 UI에서 PC/VR 모드 선택, 레벨업 특성 선택, 게임오버/클리어 후 재시작을 처리합니다.
