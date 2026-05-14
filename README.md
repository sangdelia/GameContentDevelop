# GameContentDevelop

게임콘텐츠개발 과목 팀 프로젝트 문서를 정리하는 저장소입니다.

## Project

- 프로젝트명: BLACKOUT SHIFT
- 형태: 3인 협동 멀티 잠입 익스트랙션 게임
- 개발 환경: Unity
- 개발 방식: LLM 기반 코드 보조 + 에셋 중심 제작

## Repository Structure

- `README.md`: 저장소 개요
- `docs/PROJECT_PROPOSAL.md`: 프로젝트 의견서 초안
- `docs/MVP.md`: 최소 구현 범위 정의
- `docs/TASKS.md`: 2주 작업 계획
- `docs/ASSET_PLAN.md`: 에셋 및 LLM 활용 계획
- `docs/TECH_STACK.md`: Unity 및 멀티플레이 기술 선택 기준
- `docs/START_CHECKLIST.md`: 개발 시작 체크리스트
- `docs/IMPLEMENTATION_ORDER.md`: 실제 구현 순서
- `docs/PROJECT_SETUP.md`: Unity 프로젝트 생성 및 초기 세팅 순서
- `docs/GAME_SPEC.md`: 전체 게임 명세
- `docs/SYSTEM_SPEC.md`: 시스템별 상세 명세
- `docs/COLLABORATION_GUIDE.md`: 팀 협업 규칙
- `docs/GIT_WORKFLOW.md`: 브랜치 및 병합 규칙
- `docs/FOLDER_CONVENTIONS.md`: 폴더 구조 및 네이밍 규칙
- `docs/SCRIPT_ARCHITECTURE.md`: 스크립트 책임 분리 명세
- `docs/SCENE_STRUCTURE.md`: 씬 구성 및 오브젝트 배치 명세
- `docs/NETWORK_SPEC.md`: Photon Fusion 기준 네트워크 명세
- `docs/GAME_FLOW_SPEC.md`: 라운드 진행 및 상태 전이 명세
- `docs/TEST_PLAN.md`: 기능별 테스트 계획
- `docs/INTERFACE_SPEC.md`: 인터페이스 및 메서드 시그니처 규격
- `docs/ENUM_SPEC.md`: enum 및 상태값 규격
- `docs/DATA_MODEL_SPEC.md`: ScriptableObject 및 런타임 데이터 규격
- `docs/EVENT_CONVENTIONS.md`: 이벤트 및 신호 이름 규칙
- `docs/CODING_CONVENTIONS.md`: C# 코딩 규칙
- `docs/OWNERSHIP_MATRIX.md`: 파일 및 시스템 소유 범위
- `docs/BOOTSTRAP_PLAN.md`: Unity 프로젝트 초기 구성 절차
- `docs/INITIAL_SCRIPT_LIST.md`: 첫 구현 스크립트 목록과 책임
- `docs/SPRINT_BACKLOG.md`: 구현 우선순위 기반 작업 백로그

## Development Direction

- 핵심 게임 루프 완성에 집중
- 그래픽 리소스는 기존 에셋 적극 활용
- 반복 구현과 문서화는 LLM으로 가속
- 2주 내 플레이 가능한 MVP 완성 목표

## Core Concept

BLACKOUT SHIFT는 3인이 협력하여 시설에 잠입하고 목표 물품을 회수한 뒤, 체포된 팀원을 구조하며 탈출하는 협동 멀티 잠입 익스트랙션 게임이다.
