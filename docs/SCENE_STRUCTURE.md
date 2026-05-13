# Scene Structure

## 1. 목적

공용 씬 구조를 미리 정리하여 병합 충돌과 오브젝트 배치 혼선을 줄인다.

## 2. 기본 씬 목록

### MainMenu

- 시작 메뉴
- 룸 생성 또는 입장
- 테스트 단계에서는 최소 기능만 유지

### TestRoom

- 네트워크 접속 검증용 씬
- 플레이어 스폰과 이동, 상호작용 테스트 전용

### PrototypeMap

- 실제 한 판 플레이가 일어나는 메인 씬

## 3. PrototypeMap 루트 구조 예시

- `Managers`
- `Network`
- `Environment`
- `Gameplay`
- `Spawns`
- `UI`
- `Lighting`
- `Audio`

## 4. Managers 하위 오브젝트

- `GameFlowManager`
- `AlertManager`
- `ObjectiveManager`

원칙:

- 전역 매니저만 배치
- 시각 오브젝트나 테스트용 오브젝트 배치 금지

## 5. Network 하위 오브젝트

- `NetworkBootstrap`
- `PlayerSpawnPoints`

원칙:

- Fusion Runner 관련 오브젝트는 이 그룹에 둔다
- 네트워크 디버그용 오브젝트도 이 영역에만 둔다

## 6. Environment 하위 오브젝트

- `Walls`
- `Doors`
- `Props`
- `SecurityDevices`

원칙:

- 맵 지형과 배경 오브젝트만 둔다
- 게임 상태를 직접 계산하는 스크립트는 최소화

## 7. Gameplay 하위 오브젝트

- `Items`
- `Prison`
- `ExitZone`
- `PatrolRoutes`
- `Drones`

## 8. Spawns 하위 오브젝트

- `PlayerSpawn_A`
- `PlayerSpawn_B`
- `PlayerSpawn_C`
- `DroneSpawn_01`

원칙:

- 위치 마커는 이름으로 의미가 드러나야 한다

## 9. UI 하위 오브젝트

- `HUD`
- `AlertPanel`
- `ObjectivePanel`
- `TeamStatusPanel`
- `EscapePanel`

## 10. 공용 씬 수정 규칙

- 공용 씬 수정 전 팀 채널에 공유
- 대규모 오브젝트 이동 후 바로 커밋하지 말고 테스트 후 커밋
- 테스트용 오브젝트는 `TMP_` 접두사 사용

## 11. 프리팹화 기준

- 반복 배치하는 문
- 드론
- 아이템
- 감옥 문
- 탈출 구역

## 12. 씬 분리 원칙

- 네트워크 테스트는 `TestRoom`
- 실제 플레이 루프는 `PrototypeMap`
- 메인 씬에서 기능 테스트를 동시에 하지 않는다
