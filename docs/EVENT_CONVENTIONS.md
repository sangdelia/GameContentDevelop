# Event Conventions

## 1. 목적

이벤트 이름과 사용 기준을 통일해서 상태 변화 추적과 디버깅을 쉽게 만든다.

## 2. 기본 원칙

- 이벤트 이름은 "무엇이 일어났는가"를 기준으로 작성
- 요청과 완료를 구분
- 상태 변경 이벤트와 UI 이벤트를 분리

## 3. 이름 규칙

### 권장 형식

- `OnPlayerCaptured`
- `OnAlertLevelChanged`
- `OnItemCollected`
- `OnEscapeAvailable`

### 요청 계열

- `RequestCapture`
- `RequestRescue`
- `RequestExit`

### 완료 계열

- `OnCaptureCompleted`
- `OnRescueCompleted`
- `OnExitCompleted`

## 4. 권장 이벤트 목록

### 라운드

- `OnRoundStarted`
- `OnRoundStateChanged`
- `OnRoundFailed`
- `OnRoundSucceeded`

### 플레이어

- `OnPlayerStateChanged`
- `OnPlayerCaptured`
- `OnPlayerRescued`
- `OnPlayerEscaped`

### 아이템

- `OnCarryStarted`
- `OnCarryEnded`
- `OnObjectiveValueChanged`

### Alert

- `OnAlertRaised`
- `OnAlertLevelChanged`

### AI

- `OnDroneTargetDetected`
- `OnDroneStateChanged`

## 5. 사용 규칙

- 이벤트는 상태 변경 사실을 알리는 용도로 사용
- 이벤트 리스너가 핵심 로직 소유권을 가져가면 안 됨
- 하나의 이벤트에서 너무 많은 시스템을 직접 제어하지 않음

## 6. 금지 사항

- `OnDoSomething` 같은 의미 불명확한 이름
- 이벤트 이름에 구현 디테일 노출
- 요청과 완료를 같은 이름으로 섞어 사용
