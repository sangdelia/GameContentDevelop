# Data Model Specification

## 1. 목적

게임 규칙 수치와 런타임 상태 데이터를 구분하여 수정 충돌과 하드코딩을 줄인다.

## 2. 데이터 분류

- 고정 규칙 데이터
- 씬 배치 데이터
- 런타임 상태 데이터

## 3. 고정 규칙 데이터

권장 방식:

- `ScriptableObject` 사용

대상:

- 아이템 가치
- Alert 단계별 규칙
- 드론 감지 수치
- 구조 시간
- 목표 달성 기준

## 4. 권장 데이터 자산

### ItemDefinition

포함 내용:

- 아이템 이름
- `ItemType`
- 가치
- 1인 운반 여부
- 2인 운반 필요 여부

### AlertDefinition

포함 내용:

- 단계별 이름
- AI 반응 강도
- 문 잠김 여부
- UI 표시용 색상 또는 텍스트 키

### DroneDefinition

포함 내용:

- 이동 속도
- 조사 반경
- 추적 반경
- 체포 거리

### GameRuleDefinition

포함 내용:

- 목표 가치
- 제한 시간
- 구조 시간
- 탈출 성공 최소 인원

## 5. 런타임 상태 데이터

### PlayerRuntimeState

포함 내용:

- 현재 `PlayerState`
- 현재 운반 아이템 참조
- 체포 여부
- 탈출 여부

### RoundRuntimeState

포함 내용:

- 현재 `RoundState`
- 현재 `AlertLevel`
- 현재 회수 가치
- 탈출 가능 여부

### DroneRuntimeState

포함 내용:

- 현재 `DroneState`
- 현재 타겟 참조
- 현재 조사 위치

## 6. 규칙

- 고정 수치는 매직 넘버로 스크립트에 박지 않는다
- 자주 바뀌는 밸런스 값은 데이터 자산으로 분리한다
- 런타임 상태와 에셋 정의 데이터를 한 클래스에 혼합하지 않는다

## 7. 파일 위치 권장

- `Assets/Data/Items`
- `Assets/Data/Rules`
- `Assets/Data/AI`

## 8. 금지 사항

- 아이템 가치 숫자를 스크립트 곳곳에 중복 작성
- Alert 단계 효과를 여러 스크립트에 하드코딩
- UI 표시용 텍스트를 로직 클래스에 직접 박아넣기
