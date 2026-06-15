using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool allowPcCameraKick = true;
    [SerializeField] private float kickReturnSpeed = 18f;

    public Transform Target => target;

    private Vector3 positionKick;
    private Vector3 rotationKick;

    private void LateUpdate()
    {
        SnapToTarget();
        ApplyKick();
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(target.position, target.rotation);
    }

    public void AddKick(Vector3 localPosition, Vector3 localEuler)
    {
        if (!allowPcCameraKick)
            return;

        positionKick += localPosition;
        rotationKick += localEuler;
        positionKick = Vector3.ClampMagnitude(positionKick, 0.08f);
        rotationKick = Vector3.ClampMagnitude(rotationKick, 2.2f);
    }

    private void ApplyKick()
    {
        if (!allowPcCameraKick)
            return;

        if (positionKick.sqrMagnitude <= 0.000001f && rotationKick.sqrMagnitude <= 0.000001f)
            return;

        transform.position += transform.TransformDirection(positionKick);
        transform.rotation *= Quaternion.Euler(rotationKick);

        float damping = 1f - Mathf.Exp(-kickReturnSpeed * Time.deltaTime);
        positionKick = Vector3.Lerp(positionKick, Vector3.zero, damping);
        rotationKick = Vector3.Lerp(rotationKick, Vector3.zero, damping);
    }
}
