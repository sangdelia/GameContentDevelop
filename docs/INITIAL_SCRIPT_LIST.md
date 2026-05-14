# Initial Script List

## 1. 목적

초기 구현 시 어떤 스크립트를 먼저 만들고, 각 스크립트가 어디까지 책임지는지 명확히 한다.

## 2. 1단계 필수 스크립트

### NetworkBootstrap

- 위치: `Assets/Scripts/Network`
- 책임:
  - Fusion Runner 초기화
  - 호스트 시작
  - 클라이언트 참가

### NetworkPlayerSpawner

- 위치: `Assets/Scripts/Network`
- 책임:
  - 플레이어 프리팹 스폰
  - 플레이어별 시작 위치 지정

### PlayerInputHandler

- 위치: `Assets/Scripts/Player`
- 책임:
  - 입력 수집
  - 이동 입력 전달
  - 상호작용 입력 전달

### PlayerController

- 위치: `Assets/Scripts/Player`
- 책임:
  - 이동 처리
  - 이동 가능 여부 확인

### PlayerStateController

- 위치: `Assets/Scripts/Player`
- 책임:
  - 현재 상태 저장
  - Captured 여부 반영

### PlayerInteractor

- 위치: `Assets/Scripts/Player`
- 책임:
  - 상호작용 대상 탐색
  - IInteractable 호출

### TestDoorInteractable

- 위치: `Assets/Scripts/Interaction`
- 책임:
  - 문 열기/닫기 테스트
  - 상호작용 인터페이스 검증

### TestCarryItem

- 위치: `Assets/Scripts/Items`
- 책임:
  - 운반 시작/종료 테스트
  - 동기화 검증용 아이템 동작

## 3. 2단계 필수 스크립트

### GameFlowManager

- 라운드 상태 관리
- 승패 흐름 기초

### ObjectiveManager

- 회수 가치 합산
- 목표 달성 여부 판정

### AlertManager

- Alert 단계 저장
- 상승/변화 이벤트 발행

### ExitZoneInteractable

- 탈출 가능 여부 확인
- 탈출 처리 요청

## 4. 3단계 필수 스크립트

### DroneAIController

- 드론 상태 전이

### DronePerception

- 시야 및 탐지

### DroneMovement

- 순찰 및 추적 이동

### PlayerCaptureHandler

- 체포 및 구조 복귀 처리

## 5. 생성 순서 규칙

1. 네트워크
2. 플레이어
3. 상호작용
4. 아이템
5. 라운드 관리
6. AI
7. UI

## 6. 금지 사항

- 처음부터 모든 스크립트를 한꺼번에 만들기
- 스크립트 책임 미정 상태에서 코드 작성 시작
- 테스트용 스크립트를 최종 구조에 무분별하게 남기기
