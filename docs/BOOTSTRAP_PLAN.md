# Bootstrap Plan

## 1. 목적

Unity 프로젝트를 처음 만들 때 팀원마다 다른 구조로 시작하지 않도록, 초기 구성 절차를 고정한다.

## 2. 프로젝트 생성 기준

- Unity 버전: `Unity 6.3 LTS`
- 프로젝트 이름: `BlackoutShift`
- 템플릿: `Universal 3D`
- 버전 관리 시작 지점: 프로젝트 생성 직후 첫 커밋

### Unity Hub 설치 순서

1. `Unity Hub` 실행
2. `Installs` 이동
3. `Install Editor` 클릭
4. `Unity 6.3 LTS` 선택
5. 아래 항목 체크
   - `Microsoft Visual Studio Community` 또는 코드 에디터 연동
   - `Windows Build Support (IL2CPP)`
6. `Install` 클릭

### 프로젝트 생성 순서

1. `Projects` 이동
2. `New project` 클릭
3. `Universal 3D` 선택
4. 프로젝트명 `BlackoutShift` 입력
5. 저장 경로 지정
6. `Create project` 클릭

### 생성 직후 확인

- `Console` 에러 없음
- `Assets` 폴더 정상 생성
- URP 기본 조명 표시 정상
- 팀원이 같은 Unity 버전으로 열 수 있음

## 3. 생성 직후 폴더 구조

`Assets` 아래에 아래 폴더를 먼저 만든다.

- `Assets/Scenes`
- `Assets/Scripts`
- `Assets/Scripts/Core`
- `Assets/Scripts/Player`
- `Assets/Scripts/Interaction`
- `Assets/Scripts/Items`
- `Assets/Scripts/AI`
- `Assets/Scripts/Network`
- `Assets/Scripts/UI`
- `Assets/Prefabs`
- `Assets/Prefabs/Player`
- `Assets/Prefabs/Items`
- `Assets/Prefabs/AI`
- `Assets/Prefabs/Interaction`
- `Assets/Art`
- `Assets/Audio`
- `Assets/Materials`
- `Assets/Data`
- `Assets/Data/Items`
- `Assets/Data/Rules`
- `Assets/Data/AI`
- `Assets/UI`
- `Assets/Plugins`

## 4. 첫 씬 구성

### MainMenu

- 역할: 시작 화면, 호스트 또는 참가 선택
- 초기 상태: 임시 버튼 2개 정도만 배치

### TestRoom

- 역할: 플레이어 스폰, 이동, 상호작용, 아이템 운반 테스트
- 초기 상태: 평면 바닥, 벽, 스폰 포인트 2~3개, 테스트 오브젝트 배치

### PrototypeMap

- 역할: 실제 게임 루프 구현용 메인 맵
- 초기 상태: 빈 씬에 루트 오브젝트 구조만 생성

## 5. 루트 오브젝트 규칙

`PrototypeMap`과 `TestRoom`에는 아래 루트 구조를 맞춘다.

- `Managers`
- `Network`
- `Environment`
- `Gameplay`
- `Spawns`
- `UI`
- `Lighting`
- `Audio`

### TestRoom 첫 배치 세부 기준

- 바닥: `Plane` 1개
- 벽: `Cube` 2~4개
- 스폰 포인트:
  - `PlayerSpawn_A`
  - `PlayerSpawn_B`
  - `PlayerSpawn_C`

- 각 스폰 포인트는 서로 겹치지 않게 배치
- 아직 드론, 감옥, 경보등 세부 배치는 하지 않음

## 6. 첫날 반드시 생성할 프리팹

- `PF_Player`
- `PF_TestItem`
- `PF_TestDoor`

## 7. 첫날 반드시 생성할 데이터 자산

- `GameRuleDefinition_Default`
- `AlertDefinition_Default`
- `ItemDefinition_TestItem`

## 8. 첫날 완료 기준

- Fusion Runner 연결 가능
- 호스트 생성 가능
- 클라이언트 입장 가능
- 플레이어 프리팹 스폰 성공
- 플레이어 이동 동기화 확인

## 9. 첫날 금지 항목

- 맵 디테일 배치
- 드론 연출 작업
- 경보 사운드 연출
- UI 미세 조정
- 에셋 정리 과몰입

## 10. 둘째 날 시작 조건

- 프로젝트 구조가 팀 문서와 동일함
- 첫 스폰 및 이동 테스트가 성공함
- 공용 폴더명과 씬명이 합의된 규칙과 일치함
