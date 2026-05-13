# Enum Specification

## 1. 목적

같은 상태를 서로 다른 문자열이나 숫자로 표현하는 문제를 방지한다.

## 2. PlayerState

```csharp
public enum PlayerState
{
    Idle,
    Move,
    Carry,
    Captured
}
```

규칙:

- 플레이어 이동 가능 여부는 `Captured` 여부를 우선 확인
- 추후 확장 전까지 불필요한 상태 추가 금지

## 3. AlertLevel

```csharp
public enum AlertLevel
{
    Level0,
    Level1,
    Level2,
    Level3
}
```

규칙:

- UI 문구는 enum 이름을 직접 쓰지 않고 매핑해서 표시

## 4. DroneState

```csharp
public enum DroneState
{
    Patrol,
    Investigate,
    Chase,
    Capture
}
```

## 5. RoundState

```csharp
public enum RoundState
{
    Lobby,
    Infiltration,
    Looting,
    Alerted,
    EscapeAvailable,
    Escaping,
    Success,
    Fail
}
```

## 6. ItemType

```csharp
public enum ItemType
{
    Small,
    Large
}
```

## 7. DoorState

```csharp
public enum DoorState
{
    Closed,
    Open,
    Locked
}
```

## 8. TeamMemberStatus

```csharp
public enum TeamMemberStatus
{
    Normal,
    Carrying,
    Captured,
    Escaped
}
```

## 9. 금지 사항

- 같은 의미를 문자열 상수로 중복 정의
- enum 순서에 의미를 과하게 의존
- UI 문구용 텍스트를 enum 값으로 직접 사용
