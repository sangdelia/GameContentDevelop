using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target;

    public Transform Target => target;

    private void LateUpdate()
    {
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(target.position, target.rotation);
    }
}
