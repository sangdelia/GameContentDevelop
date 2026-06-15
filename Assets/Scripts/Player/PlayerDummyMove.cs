using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDummyMove : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private LayerMask collisionMask = 1;
    [SerializeField] private float collisionSkin = 0.05f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private float cameraHeight = 1.15f;

    private float pitch;
    private CapsuleCollider capsuleCollider;

    public float MoveSpeed => moveSpeed;

    private void OnValidate()
    {
        if (collisionMask.value == 0)
        {
            collisionMask = 1;
        }

        collisionSkin = Mathf.Max(0.01f, collisionSkin);
        cameraHeight = Mathf.Max(0.1f, cameraHeight);
    }

    public void AddMoveSpeedMultiplier(float multiplier)
    {
        moveSpeed *= multiplier;
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        pitch = 0f;

        if (cameraPoint != null)
        {
            cameraPoint.localPosition = Vector3.up * cameraHeight;
            cameraPoint.localRotation = Quaternion.identity;
        }
    }

    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        ApplyRuntimeDefaults();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPoint == null)
        {
            Debug.LogError("PlayerDummyMove: CameraPoint is not assigned.");
        }
        else if (cameraPoint.localPosition.sqrMagnitude < 0.01f)
        {
            cameraPoint.localPosition = Vector3.up * cameraHeight;
        }
    }

    private void ApplyRuntimeDefaults()
    {
        if (collisionMask.value == 0)
        {
            collisionMask = 1;
        }

        if (collisionSkin <= 0f)
        {
            collisionSkin = 0.05f;
        }

        if (cameraHeight <= 0f)
        {
            cameraHeight = 1.15f;
        }
    }

    private void Update()
    {
        Move();
        Look();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Move()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        MoveWithCollision(move * moveSpeed * Time.deltaTime);
    }

    private void MoveWithCollision(Vector3 movement)
    {
        if (movement.sqrMagnitude <= 0.000001f)
            return;

        if (capsuleCollider == null)
        {
            transform.position += movement;
            return;
        }

        Vector3 direction = movement.normalized;
        float distance = movement.magnitude;

        if (!CapsuleCast(direction, distance + collisionSkin, out RaycastHit hit))
        {
            transform.position += movement;
            return;
        }

        float allowedDistance = Mathf.Max(0f, hit.distance - collisionSkin);
        transform.position += direction * allowedDistance;

        Vector3 remaining = movement - direction * allowedDistance;
        Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
        slide.y = 0f;

        if (slide.sqrMagnitude > 0.000001f && !CapsuleCast(slide.normalized, slide.magnitude + collisionSkin, out _))
        {
            transform.position += slide;
        }
    }

    private bool CapsuleCast(Vector3 direction, float distance, out RaycastHit hit)
    {
        GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius);

        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1,
            point2,
            radius,
            direction,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        hit = default;
        float nearestDistance = float.MaxValue;
        bool foundHit = false;

        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider == null)
                continue;

            if (candidate.collider == capsuleCollider || candidate.collider.transform.IsChildOf(transform))
                continue;

            if (ShouldIgnoreMovementHit(candidate))
                continue;

            if (candidate.distance < nearestDistance)
            {
                nearestDistance = candidate.distance;
                hit = candidate;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private bool ShouldIgnoreMovementHit(RaycastHit hit)
    {
        if (hit.normal.y > 0.45f)
            return true;

        GameObject hitObject = hit.collider.gameObject;

        if (hit.collider.GetComponentInParent<TempBossController>() != null)
            return false;

        if (hit.collider.GetComponentInParent<EnemyHealth>() != null)
            return false;

        if (hitObject.CompareTag("Ground"))
            return hit.normal.y > 0.45f;

        string objectName = hitObject.name;
        return (objectName.Contains("Ground") || objectName.Contains("Floor")) && hit.normal.y > 0.45f;
    }

    private void GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius)
    {
        Vector3 center = transform.TransformPoint(capsuleCollider.center);
        float height = Mathf.Max(capsuleCollider.height * transform.lossyScale.y, 0.01f);
        radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float cylinderHeight = Mathf.Max(0f, height * 0.5f - radius);

        point1 = center + Vector3.up * cylinderHeight;
        point2 = center - Vector3.up * cylinderHeight;
    }

    private void Look()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();

        float yaw = mouseDelta.x * mouseSensitivity;
        transform.Rotate(0f, yaw, 0f);

        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        if (cameraPoint != null)
        {
            cameraPoint.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
