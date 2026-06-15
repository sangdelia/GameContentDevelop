using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SimpleQuest2VrRig : MonoBehaviour
{
    [Header("Rig")]
    [SerializeField] private Transform locomotionRoot;
    [SerializeField] private Camera xrCamera;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float fallbackCameraHeight = 1.6f;

    private bool configured;

    public Camera XrCamera => xrCamera;
    public Transform RightController => rightController != null ? rightController : xrCamera != null ? xrCamera.transform : transform;

    public void Configure(Transform root, Camera camera, Transform leftHand, Transform rightHand)
    {
        locomotionRoot = root;
        xrCamera = camera;
        leftController = leftHand;
        rightController = rightHand;
        configured = true;
    }

    private void Awake()
    {
        if (locomotionRoot == null)
        {
            locomotionRoot = transform.parent != null ? transform.parent : transform;
        }
    }

    private void OnEnable()
    {
        configured = true;
    }

    private void Update()
    {
        if (!configured)
            return;

        UpdateTrackedPose("<XRHMD>", xrCamera != null ? xrCamera.transform : null, Vector3.up * fallbackCameraHeight);
        UpdateTrackedPose("<XRController>{LeftHand}", leftController, new Vector3(-0.28f, 1.15f, 0.42f));
        UpdateTrackedPose("<XRController>{RightHand}", rightController, new Vector3(0.28f, 1.15f, 0.42f));
        MoveFromLeftStick();
    }

    private void UpdateTrackedPose(string devicePath, Transform target, Vector3 fallbackLocalPosition)
    {
        if (target == null)
            return;

        InputDevice device = InputSystem.GetDevice(devicePath);
        if (device == null)
        {
            target.localPosition = fallbackLocalPosition;
            target.localRotation = Quaternion.identity;
            return;
        }

        Vector3Control positionControl = device.TryGetChildControl<Vector3Control>("devicePosition");
        QuaternionControl rotationControl = device.TryGetChildControl<QuaternionControl>("deviceRotation");

        target.localPosition = positionControl != null ? positionControl.ReadValue() : fallbackLocalPosition;
        target.localRotation = rotationControl != null ? rotationControl.ReadValue() : Quaternion.identity;
    }

    private void MoveFromLeftStick()
    {
        if (locomotionRoot == null || xrCamera == null)
            return;

        InputDevice leftDevice = InputSystem.GetDevice("<XRController>{LeftHand}");
        if (leftDevice == null)
            return;

        Vector2Control stick = leftDevice.TryGetChildControl<Vector2Control>("thumbstick");
        if (stick == null)
        {
            stick = leftDevice.TryGetChildControl<Vector2Control>("primary2DAxis");
        }

        if (stick == null)
            return;

        Vector2 input = stick.ReadValue();
        if (input.sqrMagnitude < 0.01f)
            return;

        Vector3 forward = xrCamera.transform.forward;
        Vector3 right = xrCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 movement = forward * input.y + right * input.x;
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        locomotionRoot.position += movement * moveSpeed * Time.deltaTime;
    }
}
