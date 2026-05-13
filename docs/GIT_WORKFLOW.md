# Git Workflow

## 1. 기본 원칙

- `main`은 항상 실행 가능한 상태를 유지한다.
- 직접 `main`에 큰 작업을 올리지 않는다.
- 기능 단위 브랜치에서 작업 후 병합한다.

## 2. 브랜치 규칙

### 브랜치 이름 예시

- `feature/player-move`
- `feature/network-room`
- `feature/drone-ai`
- `fix/item-sync`
- `docs/spec-update`

## 3. 작업 절차

1. `main` 최신 pull
2. 새 브랜치 생성
3. 기능 구현
4. 로컬 테스트
5. 커밋
6. 공유 또는 병합

## 4. 커밋 규칙

### 예시

- `Add player movement prototype`
- `Implement Fusion room join flow`
- `Fix item carry sync issue`
- `Update game specification`

## 5. 병합 규칙

- 공용 씬 충돌 시 담당자끼리 먼저 조정
- 병합 전 최소 1회 실행 테스트
- 멀티 기능은 2클라이언트 기준 확인 후 병합

## 6. 금지 사항

- 설명 없는 대규모 커밋
- 테스트 없이 main 병합
- 다른 팀원 작업 브랜치 강제 덮어쓰기
- 프로젝트 설정 파일 무단 변경
