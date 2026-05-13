# Ownership Matrix

## 1. 목적

누가 어떤 시스템과 파일을 우선 담당하는지 명확히 해서 동시 수정 충돌을 줄인다.

## 2. 담당 기준

### 팀원 A

주 담당:

- `Assets/Scripts/Player`
- `Assets/Scripts/Interaction`
- `Assets/Scripts/Items`

공용 협의 대상:

- 플레이어 프리팹
- 상호작용 인터페이스

### 팀원 B

주 담당:

- `Assets/Scripts/Network`
- Fusion Runner 설정
- 플레이어 스폰 및 상태 동기화

공용 협의 대상:

- 전역 게임 상태
- 공용 네트워크 프리팹

### 팀원 C

주 담당:

- `Assets/Scripts/AI`
- `Assets/Scripts/UI`
- 맵 배치 및 씬 연출

공용 협의 대상:

- Alert UI
- 드론 프리팹
- PrototypeMap 씬

## 3. 공동 소유 파일

아래는 단독 수정 금지 대상으로 본다.

- `GameFlowManager`
- `AlertManager`
- `ObjectiveManager`
- `PrototypeMap` 씬
- 입력 설정 파일
- 프로젝트 설정 파일

## 4. 수정 절차

1. 담당자 확인
2. 공용 파일 여부 확인
3. 공용 파일이면 먼저 공유
4. 수정 후 테스트
5. 변경 범위 공유 후 병합

## 5. 충돌 발생 시 우선순위

- 네트워크 안정성
- 한 판 플레이 가능 여부
- 시연 안정성
- 연출 및 편의성

## 6. 금지 사항

- 다른 팀원 담당 폴더를 대량 수정 후 무통보 push
- 공용 씬을 동시에 여러 명이 장시간 수정
- 책임 경계 없이 임시 수정 반복
