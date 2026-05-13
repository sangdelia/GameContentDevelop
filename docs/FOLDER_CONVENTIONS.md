# Folder And Naming Conventions

## 1. Assets 폴더 구조

- `Assets/Scenes`
- `Assets/Scripts`
- `Assets/Prefabs`
- `Assets/Materials`
- `Assets/Art`
- `Assets/Audio`
- `Assets/UI`
- `Assets/Network`
- `Assets/Plugins`

## 2. Scripts 하위 구조

- `Assets/Scripts/Core`
- `Assets/Scripts/Player`
- `Assets/Scripts/Interaction`
- `Assets/Scripts/Items`
- `Assets/Scripts/AI`
- `Assets/Scripts/UI`
- `Assets/Scripts/Network`

## 3. 씬 이름 규칙

- `MainMenu`
- `TestRoom`
- `PrototypeMap`

## 4. 프리팹 이름 규칙

- `PF_Player`
- `PF_Drone`
- `PF_SmallItem`
- `PF_LargeItem`
- `PF_PrisonDoor`

## 5. 스크립트 이름 규칙

- 클래스명과 파일명 일치
- PascalCase 사용
- 역할이 드러나게 작성

### 예시

- `PlayerController`
- `PlayerInteractor`
- `DroneAIController`
- `AlertManager`
- `GameFlowManager`

## 6. 공용 매니저 규칙

- 전역 시스템은 `Manager` 접미사 사용
- 한 파일에 여러 책임을 넣지 않는다

## 7. 테스트 오브젝트 규칙

- 임시 테스트 오브젝트는 이름 앞에 `TMP_` 사용
- 최종 반영 전 정리 여부 확인
