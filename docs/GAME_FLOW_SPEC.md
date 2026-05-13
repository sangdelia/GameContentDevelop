# Game Flow Specification

## 1. 목적

라운드가 어떤 상태를 거쳐 진행되는지 명확히 하여 시스템 연결 기준을 고정한다.

## 2. 라운드 상태

- Lobby
- Infiltration
- Looting
- Alerted
- EscapeAvailable
- Escaping
- Success
- Fail

## 3. 상태 설명

### Lobby

- 플레이어 접속 대기
- 준비 완료 후 게임 시작

### Infiltration

- 시설 진입 직후 상태
- 아직 목표 가치를 충분히 확보하지 못한 상태

### Looting

- 아이템 탐색 및 회수 진행
- 기본 플레이 루프 중심 상태

### Alerted

- Alert Level이 일정 이상 올라가 위험도가 높아진 상태
- 드론 추적과 문 잠김이 더 적극적으로 개입

### EscapeAvailable

- 목표 가치 달성
- 탈출 구역으로 이동 가능

### Escaping

- 탈출 시도 중
- 체포와 방해가 여전히 가능

### Success

- 최소 1명 이상 탈출 성공

### Fail

- 전원 체포
- 시간 초과
- 행동 불가 상태

## 4. 주요 전이

- `Lobby -> Infiltration`
- `Infiltration -> Looting`
- `Looting -> Alerted`
- `Looting -> EscapeAvailable`
- `Alerted -> EscapeAvailable`
- `EscapeAvailable -> Escaping`
- `Escaping -> Success`
- `Any Active State -> Fail`

## 5. 이벤트 기준

### Alert 상승 이벤트

- 소음 발생
- 감지 장치 노출
- 구조 시도

### 체포 이벤트

- 드론이 플레이어를 포착하고 체포 성공

### 목표 달성 이벤트

- 회수 가치가 기준 이상이 됨

### 탈출 이벤트

- 탈출 가능 상태에서 플레이어가 탈출 구역 도달

## 6. 상태별 활성 시스템

### Infiltration / Looting

- 이동
- 상호작용
- 회수
- 기본 드론 순찰

### Alerted

- 강화된 드론 추적
- 문 잠김 일부 활성화
- 긴장감 연출 강화

### EscapeAvailable / Escaping

- 탈출 UI 표시
- 탈출 구역 활성
- 마지막 방해 요소 유지

## 7. 상태 전이 책임

- 최종 라운드 상태 변경은 `GameFlowManager`가 담당
- 각 시스템은 상태 변경 요청만 하고 직접 전체 라운드 상태를 바꾸지 않는다
