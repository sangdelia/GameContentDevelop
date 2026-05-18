# Tech Stack

## 목표

팀이 개발을 시작하기 전에 Unity 버전, 멀티플레이 방식, 작업 원칙을 고정한다.

## 확정 선택

- Unity: `Unity 6.3 LTS`
- 멀티플레이: `Photon Fusion`
- 렌더 파이프라인: `URP`
- 입력 시스템: `Unity Input System`

## 확정 이유

- Unity 6 LTS는 새 프로젝트 기준으로 안정적인 선택지다.
- Photon Fusion은 협동 멀티 프로토타입에 적합하고 2~3인 테스트 흐름을 잡기 좋다.
- URP는 어두운 시설, 경보등, 잠입 분위기 연출에 유리하다.
- Input System은 이후 키 바인딩과 입력 확장에 대응하기 쉽다.

## 선택 대안과 제외 이유

### Netcode for GameObjects 제외 이유

- Unity 공식 생태계라는 장점은 있지만, 2주 안에 결과를 내는 과제 기준에서는 Photon Fusion보다 초반 시행착오 가능성이 더 크다고 판단했다.

### Built-in 제외 이유

- 에셋 호환성 면에서는 유리할 수 있지만, 이번 프로젝트는 잠입 분위기와 경보 연출이 중요하므로 URP를 우선 선택한다.

에셋 호환성 문제가 큰 경우에만 Built-in 전환을 재검토한다.

## 세부 기준

### Unity 6.3 LTS

- 원칙: 팀원 전원이 같은 에디터 버전 사용
- Unity Hub `Installs`에서 같은 LTS 버전 설치
- 프로젝트 생성 시 `Universal 3D` 템플릿 사용
- 프로젝트 생성 직후 렌더 파이프라인 혼용 금지

### Unity 설치 모듈 기준

- 필수 권장:
  - `Microsoft Visual Studio Community` 또는 코드 에디터 연동 항목
  - `Windows Build Support (IL2CPP)`

- 현재 단계에서 선택 제외:
  - `Android Build Support`
  - `iOS Build Support`
  - `WebGL Build Support`

### Unity Hub 기준 설치 절차

1. `Unity Hub` 실행
2. 왼쪽 `Installs` 선택
3. `Install Editor` 또는 `Add` 클릭
4. `Unity 6.3 LTS` 선택
5. 권장 모듈 체크
6. `Install` 클릭

### 프로젝트 생성 기준

1. 왼쪽 `Projects` 선택
2. `New project` 클릭
3. `Universal 3D` 선택
4. 프로젝트명 `BlackoutShift`
5. 저장 경로 지정
6. `Create project` 클릭

### Photon Fusion

- 룸 생성 및 입장 흐름을 가장 먼저 검증
- 이동, 스폰, 상호작용 동기화를 우선 구현
- 고급 기능보다 기본 세션 안정성 우선

### URP

- 조명, 경보등, 어두운 환경 연출에 집중
- 에셋 임포트 시 머티리얼 호환 여부를 먼저 확인
- 후반부에 포스트 프로세싱은 최소한으로 추가

## 작업 원칙

- 처음부터 예쁘게 만들지 않는다
- 멀티 접속과 핵심 루프가 먼저다
- 로컬에서 되는 기능만 만들지 말고 동기화 기준으로 구현한다
- LLM이 생성한 코드는 바로 넣지 말고 작은 단위로 검증한다

## 오늘 확정해야 할 항목

- Unity 6.3 LTS 사용
- Photon Fusion 사용
- URP 사용
- 브랜치 전략
- 씬 이름 규칙
- 프리팹 및 스크립트 폴더 구조
