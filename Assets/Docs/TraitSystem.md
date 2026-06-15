# STARGRAVE SURVIVOR 특성 시스템

이 문서는 Unity 에디터 안에서 바로 확인하기 위한 요약 문서입니다. 더 자세한 설명은 루트 문서 `docs/TRAIT_SYSTEM.md`를 참고합니다.

## 핵심 흐름

1. `PlayerLevel`이 레벨업 이벤트를 발생시킵니다.
2. `StargraveRuntimeUI`가 특성 선택 패널을 표시합니다.
3. `PlayerTraitController.RollChoices()`가 선택지 3개를 뽑습니다.
4. 플레이어가 특성을 선택하면 `PlayerTraitController.ApplyTrait()`가 효과를 적용합니다.
5. 게임 시간이 다시 흐르고 선택한 특성이 전투에 반영됩니다.

## 핵심 스크립트

- `Assets/Scripts/Traits/TraitData.cs`: 특성 데이터 구조
- `Assets/Scripts/Traits/PlayerTraitController.cs`: 특성 선택, 레벨 저장, 효과 적용
- `Assets/Scripts/Traits/PlayerCombatStats.cs`: 피해량과 공격 속도 계산
- `Assets/Scripts/Traits/StatusEffectController.cs`: 화상, 둔화 상태이상 처리
- `Assets/Scripts/Traits/PlayerAuraController.cs`: 주변 적 둔화 오라 처리
- `Assets/Scripts/Player/PlayerShootTest.cs`: 사격 명중 후 특성 효과 호출
- `Assets/Scripts/UI/StargraveRuntimeUI.cs`: 레벨업 선택 UI 표시

## 탄환 속성

탄환 속성은 마지막으로 선택한 것 하나만 적용됩니다.

- 일반탄: 노란색 레이
- 화염탄: 빨간색 레이, 명중 시 화상
- 빙결탄: 파란색/하늘색 레이, 명중 시 둔화

화염탄을 선택하면 빙결탄은 꺼지고, 빙결탄을 선택하면 화염탄은 꺼집니다.

## 현재 특성

- 화염탄: 탄환을 화염 속성으로 변경하고 화상 피해를 부여합니다.
- 빙결탄: 탄환을 얼음 속성으로 변경하고 적 이동 속도를 늦춥니다.
- 피해 강화: 무기 피해량을 증가시킵니다.
- 공격 속도 강화: 발사 속도를 증가시킵니다.
- 이동 속도 강화: PC/VR 이동 속도를 증가시킵니다.
- 경험치 자석: 경험치 오브 흡입 범위를 증가시킵니다.
- 최대 체력 증가: 최대 체력을 증가시킵니다.
- 방어력: 받는 피해를 고정 수치만큼 줄입니다.
- 체력 재생: 초당 체력 재생량을 추가합니다.
- 처치 회복: 적 처치 시 체력을 회복합니다.
- 보호막: 일정 시간마다 피해 1회를 막습니다.
- 중력 덫: 주변 적에게 둔화 오라를 적용합니다.
- 처형 탄두: 일반 적 명중 시 확률적으로 즉시 처치합니다.

## 새 특성 추가 순서

1. `TraitEffectKind`에 효과 종류를 추가합니다.
2. `PlayerTraitController.ApplyTrait()`에 적용 로직을 추가합니다.
3. 직접 명중 효과라면 `HandleDirectEnemyHit()`에 추가합니다.
4. 지속 효과라면 `StatusEffectController`를 확장합니다.
5. `TraitData` 에셋 또는 `EnsureDefaultCatalog()`에 특성을 등록합니다.
6. UI 표시 이름과 설명을 갱신합니다.

## 설계 메모

- 특성 규칙은 PC와 VR에서 동일해야 합니다.
- 입력 처리와 특성 효과는 분리합니다.
- 상태이상은 적에게 필요할 때만 동적으로 붙입니다.
- 보스에게 즉사 효과는 적용하지 않습니다.
- UI 문구는 한글 중심으로 유지합니다.
