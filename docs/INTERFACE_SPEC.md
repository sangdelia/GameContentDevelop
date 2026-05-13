# Interface Specification

## 1. 목적

팀원이 같은 시스템을 구현할 때 인터페이스 이름과 메서드 구조가 달라지는 문제를 막는다.

## 2. 기본 원칙

- 인터페이스 이름은 `I` 접두사 사용
- 기능은 최소 메서드 집합으로 정의
- 네트워크 구현 세부사항은 인터페이스에 직접 노출하지 않는다
- 인터페이스는 사용 목적이 드러나게 작성한다

## 3. 상호작용 인터페이스

### IInteractable

용도:

- 플레이어가 상호작용 가능한 모든 오브젝트의 공통 인터페이스

권장 메서드:

```csharp
public interface IInteractable
{
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
    string GetInteractionPrompt();
}
```

규칙:

- `CanInteract`는 실제 상호작용 가능 여부만 판단
- `Interact` 안에서 UI 갱신을 직접 처리하지 않음
- 프롬프트 문구는 UI 쪽에서 그대로 표시 가능해야 함

## 4. 운반 인터페이스

### ICarryable

용도:

- 플레이어가 운반 가능한 오브젝트 공통 인터페이스

권장 메서드:

```csharp
public interface ICarryable
{
    bool CanPickup(PlayerCarryHandler carrier);
    void BeginCarry(PlayerCarryHandler carrier);
    void EndCarry(PlayerCarryHandler carrier);
}
```

규칙:

- 운반 시작과 종료는 반드시 짝이 맞아야 함
- 운반 처리 중 가치 계산을 직접 수행하지 않음

## 5. 감지 인터페이스

### INoiseEmitter

용도:

- Alert 시스템과 연결될 수 있는 소음 발생원

권장 메서드:

```csharp
public interface INoiseEmitter
{
    float GetNoiseRadius();
    int GetNoiseAlertValue();
}
```

## 6. 구조 대상 인터페이스

### IRescuable

용도:

- 구조 가능한 대상 규격 통일

권장 메서드:

```csharp
public interface IRescuable
{
    bool CanRescue(PlayerInteractor interactor);
    void BeginRescue(PlayerInteractor interactor);
    void CancelRescue(PlayerInteractor interactor);
    void CompleteRescue(PlayerInteractor interactor);
}
```

규칙:

- 구조 진행 시간은 구현체가 아니라 규칙 데이터에서 가져오는 것이 바람직

## 7. 상태 조회 인터페이스

### IPlayerStateReadable

용도:

- UI나 AI가 플레이어 상태를 읽을 때 직접 내부 구현에 강하게 결합되지 않도록 하기 위함

권장 메서드:

```csharp
public interface IPlayerStateReadable
{
    PlayerState GetCurrentState();
    bool IsCaptured();
    bool IsCarrying();
}
```

## 8. 금지 사항

- 인터페이스에 불필요한 setter 다수 노출
- UI 전용 메서드를 게임 규칙 인터페이스에 섞기
- 하나의 인터페이스가 상호작용, 운반, 상태 변경까지 모두 담당하게 만들기
