# Project Setup Guide

## 기준 환경

- Unity: `Unity 6.3 LTS`
- Template: `Universal 3D`
- Network: `Photon Fusion`
- Input: `Unity Input System`

## 1. Unity 프로젝트 생성

- Unity Hub `Projects`에서 `New project` 클릭
- 템플릿은 `Universal 3D` 선택
- 프로젝트 이름은 `BlackoutShift` 권장
- 저장 위치는 팀원이 합의한 작업 폴더로 통일
- 생성 전 Unity 버전이 `Unity 6.3 LTS`인지 확인

### Unity Hub 에디터 설치 기준

- `Installs` 탭에서 `Unity 6.3 LTS` 설치
- 함께 설치할 항목:
  - `Microsoft Visual Studio Community` 또는 사용 코드 에디터 연동
  - `Windows Build Support (IL2CPP)`

- 지금은 설치하지 않아도 되는 항목:
  - Android
  - iOS
  - WebGL

### 프로젝트 생성 후 즉시 확인할 것

- 에디터 상단에 `Unity 6.3 LTS`로 열렸는지 확인
- `Console`에 빨간 에러가 없는지 확인
- 기본 샘플 씬이 정상적으로 렌더링되는지 확인

## 2. 첫 폴더 구조

프로젝트 생성 직후 아래 구조를 만든다.

- `Assets/Scenes`
- `Assets/Scripts`
- `Assets/Prefabs`
- `Assets/Materials`
- `Assets/Art`
- `Assets/Audio`
- `Assets/UI`
- `Assets/Network`
- `Assets/Plugins`

## 3. 기본 씬 구성

- `MainMenu` 씬
- `TestRoom` 씬
- `PrototypeMap` 씬

초기에는 `TestRoom` 하나만으로 멀티 접속을 검증해도 충분하지만, 씬 파일은 처음부터 3개를 만들어 두는 편이 협업 충돌이 적다.

### 씬 생성 절차

1. `Assets/Scenes` 폴더 생성
2. 현재 열린 기본 씬을 `File > Save As`로 `TestRoom` 저장
3. `Assets/Scenes`에서 새 씬 생성
4. 이름을 `MainMenu`, `PrototypeMap`으로 각각 저장

## 4. 패키지 및 기능 세팅

- Input System 활성화
- Photon Fusion 패키지 임포트
- 필요 시 TextMeshPro 기본 세팅 확인
- URP 기본 조명과 카메라 설정 점검

### Input System 활성화 절차

1. `Edit > Project Settings > Player`
2. `Active Input Handling` 항목 확인
3. `Input System Package (New)` 또는 필요 시 `Both` 선택
4. 에디터 재시작 요구 시 재시작

### 초기 테스트 씬 최소 구성

- Plane 바닥 1개
- 벽 Cube 2~4개
- `PlayerSpawn_A`, `PlayerSpawn_B`, `PlayerSpawn_C`
- `Managers`, `Network`, `Environment`, `Gameplay`, `Spawns`, `UI`, `Lighting`, `Audio` 루트 오브젝트

## 5. 첫날 구현 목표

- 플레이어 프리팹 생성
- 네트워크 스폰 연결
- 이동 구현
- 카메라 연결
- 2명 이상 접속 테스트

## 6. 첫날 완료 기준

- 호스트 생성 가능
- 클라이언트 접속 가능
- 서로의 플레이어가 보임
- 이동이 동기화됨
- 씬 재실행 시 큰 오류가 없음

## 7. 첫날 금지 사항

- 적 AI 구현
- 경보 시스템 구현
- 감옥 시스템 구현
- 고급 UI 제작
- 에셋 디테일 정리

## 8. 둘째 날 넘어갈 조건

- 멀티 접속이 재현 가능함
- 플레이어 이동과 스폰이 안정적임
- 테스트용 상호작용 오브젝트 1개를 네트워크 환경에서 다룰 수 있음
