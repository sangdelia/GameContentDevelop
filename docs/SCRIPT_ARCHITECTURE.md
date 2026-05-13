# Script Architecture

## 1. 목적

스크립트 역할을 미리 고정하여 팀원 간 구현 중복과 책임 충돌을 줄인다.

## 2. 설계 원칙

- 한 스크립트는 하나의 책임만 가진다.
- 입력, 이동, 상호작용, 상태 판정, UI 갱신을 한 파일에 섞지 않는다.
- 네트워크 동기화 책임과 로컬 표현 책임을 가능하면 분리한다.
- 공용 매니저는 최소화하고, 필요한 경우에만 사용한다.

## 3. Core 계층

### GameFlowManager

- 라운드 시작, 진행, 종료 상태 관리
- 승리 및 실패 판정 요청
- Alert, 목표 가치, 플레이어 상태 등 전역 시스템 연결

### AlertManager

- Alert Level 관리
- Alert 상승 및 하강 규칙 적용
- 문 잠김, AI 반응 강도 변경 이벤트 발행

### ObjectiveManager

- 회수한 아이템 가치 합산
- 목표 달성 여부 판단
- 탈출 가능 상태 갱신

### SessionContext

- 현재 라운드의 공용 참조 보관
- 매니저 간 참조 연결 보조

## 4. Player 계층

### PlayerController

- 플레이어 이동 처리
- 이동 가능 여부 판단
- Captured 상태일 때 이동 차단

### PlayerInputHandler

- Input System 입력 수신
- 이동, 상호작용 요청을 각 담당 스크립트로 전달
- 직접 게임 규칙을 처리하지 않음

### PlayerInteractor

- 상호작용 대상 탐색
- 상호작용 가능 여부 판단
- Interactable 인터페이스 호출

### PlayerCarryHandler

- 운반 상태 관리
- 소형 아이템 운반 처리
- 대형 아이템 참여 상태 연결

### PlayerStateController

- Idle, Move, Carry, Captured 상태 관리
- 상태 전이에 따른 제약 적용

### PlayerCaptureHandler

- 적에게 잡혔을 때 체포 처리
- 감옥 이동 요청
- 구조 완료 시 상태 복귀

## 5. Interaction 계층

### IInteractable

- 모든 상호작용 오브젝트 공통 인터페이스
- `CanInteract`
- `Interact`

### DoorInteractable

- 일반 문 열기 또는 닫기 처리
- 잠김 상태 확인

### PrisonDoorInteractable

- 감옥 구조 상호작용 처리
- 일정 시간 유지 필요
- 성공 시 구조 이벤트 발행

### ExitZoneInteractable

- 탈출 조건 만족 여부 확인
- 탈출 처리 요청

## 6. Items 계층

### ItemBase

- 공통 아이템 데이터 보관
- 가치, 타입, 파손 가능 여부 관리

### SmallCarryItem

- 단독 운반 아이템 처리

### LargeCarryItem

- 2인 운반 상태 관리
- 참여 플레이어 수 확인

### ItemDropHandler

- 아이템 놓기 처리
- 낙하 또는 소음 발생 처리 연결 가능

## 7. AI 계층

### DroneAIController

- 드론 상태 전이 관리
- Patrol, Investigate, Chase, Capture 수행

### DronePerception

- 시야, 거리, 감지 판단
- 플레이어 또는 소음 위치 인식

### DroneMovement

- 이동 경로 추종
- 순찰 및 조사 위치 이동

### DroneCaptureHandler

- 플레이어 체포 판정
- 체포 성공 시 PlayerCaptureHandler 호출

## 8. Network 계층

### NetworkBootstrap

- Fusion Runner 초기화
- 호스트 또는 클라이언트 시작 처리

### NetworkPlayerSpawner

- 플레이어 프리팹 스폰
- 접속 시 플레이어 생성

### NetworkStateSync

- 게임 전역 상태 동기화 담당
- Alert, 목표 상태, 라운드 상태 공유

### NetworkEventRelay

- RPC 또는 네트워크 이벤트 전달 보조
- 구조, 탈출, 경보 상승 같은 이벤트 연결

## 9. UI 계층

### AlertUIController

- 현재 Alert 단계 표시

### ObjectiveUIController

- 회수 가치 및 목표 진행 표시

### TeamStatusUIController

- 팀원별 정상, 운반, 체포 상태 표시

### EscapeUIController

- 탈출 가능 여부 표시

## 10. 금지 사항

- PlayerController 안에 아이템 가치 계산 넣지 않기
- AlertManager 안에 UI 직접 갱신 넣지 않기
- GameFlowManager에 드론 이동 로직 넣지 않기
- 하나의 스크립트가 네트워크, UI, 물리, 입력을 동시에 처리하지 않기
