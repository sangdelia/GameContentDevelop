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

    [Header("Controller Visuals")]
    [SerializeField] private bool showControllerVisuals = true;
    [SerializeField] private string controllerPrefabResourcePath = "Models/VR/Quest2Controller";
    [SerializeField] private Vector3 controllerVisualLocalPosition = new Vector3(0f, -0.025f, 0.055f);
    [SerializeField] private Vector3 controllerVisualLocalRotation = new Vector3(16f, 0f, 0f);
    [SerializeField] private Vector3 controllerVisualLocalScale = Vector3.one;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float fallbackCameraHeight = 1.6f;
    [SerializeField] private LayerMask collisionMask = 1;
    [SerializeField] private float collisionSkin = 0.05f;

    private bool configured;
    private bool visualsBuilt;
    private Transform leftControllerVisual;
    private Transform rightControllerVisual;
    private CapsuleCollider locomotionCollider;

    public Camera XrCamera => xrCamera;
    public Transform RightController => rightController != null ? rightController : xrCamera != null ? xrCamera.transform : transform;

    public void Configure(Transform root, Camera camera, Transform leftHand, Transform rightHand)
    {
        locomotionRoot = root;
        xrCamera = camera;
        leftController = leftHand;
        rightController = rightHand;
        locomotionCollider = locomotionRoot != null ? locomotionRoot.GetComponent<CapsuleCollider>() : null;
        configured = true;
    }

    private void Awake()
    {
        if (locomotionRoot == null)
        {
            locomotionRoot = transform.parent != null ? transform.parent : transform;
        }

        locomotionCollider = locomotionRoot != null ? locomotionRoot.GetComponent<CapsuleCollider>() : null;
    }

    private void OnEnable()
    {
        configured = true;
        EnsureControllerVisuals();
    }

    private void Update()
    {
        if (!configured)
            return;

        UpdateTrackedPose("<XRHMD>", xrCamera != null ? xrCamera.transform : null, Vector3.up * fallbackCameraHeight);
        UpdateTrackedPose("<XRController>{LeftHand}", leftController, new Vector3(-0.28f, 1.15f, 0.42f));
        UpdateTrackedPose("<XRController>{RightHand}", rightController, new Vector3(0.28f, 1.15f, 0.42f));
        EnsureControllerVisuals();
        MoveFromLeftStick();
    }

    private void EnsureControllerVisuals()
    {
        if (visualsBuilt || !showControllerVisuals)
            return;

        if (leftController != null)
        {
            leftControllerVisual = CreateControllerVisual(leftController, true);
        }

        if (rightController != null)
        {
            rightControllerVisual = CreateControllerVisual(rightController, false);
        }

        visualsBuilt = leftControllerVisual != null || rightControllerVisual != null;
    }

    private Transform CreateControllerVisual(Transform parent, bool isLeftHand)
    {
        GameObject prefab = Resources.Load<GameObject>(controllerPrefabResourcePath);

        GameObject visualRoot = prefab != null
            ? Instantiate(prefab, parent)
            : BuildFallbackQuestControllerVisual(parent, isLeftHand);

        visualRoot.name = isLeftHand ? "VR_Left_Quest2_Controller_Visual" : "VR_Right_Quest2_Controller_Visual";
        visualRoot.transform.localPosition = controllerVisualLocalPosition;
        visualRoot.transform.localRotation = Quaternion.Euler(controllerVisualLocalRotation);
        visualRoot.transform.localScale = controllerVisualLocalScale;

        if (isLeftHand)
        {
            visualRoot.transform.localScale = new Vector3(
                -Mathf.Abs(visualRoot.transform.localScale.x),
                visualRoot.transform.localScale.y,
                visualRoot.transform.localScale.z
            );
        }

        Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        return visualRoot.transform;
    }

    private GameObject BuildFallbackQuestControllerVisual(Transform parent, bool isLeftHand)
    {
        GameObject root = new GameObject(isLeftHand ? "GeneratedQuest2LeftController" : "GeneratedQuest2RightController");
        root.transform.SetParent(parent, false);

        Material shellMaterial = CreateControllerMaterial(new Color(0.88f, 0.9f, 0.92f), 0f);
        Material darkMaterial = CreateControllerMaterial(new Color(0.05f, 0.055f, 0.065f), 0f);
        Material glowMaterial = CreateControllerMaterial(new Color(0.08f, 0.78f, 1f), 1.4f);

        CreatePrimitivePart(root.transform, "Grip", PrimitiveType.Capsule, new Vector3(0f, -0.08f, 0f), Quaternion.Euler(8f, 0f, 0f), new Vector3(0.075f, 0.17f, 0.075f), shellMaterial);
        CreatePrimitivePart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.045f, 0.035f), Quaternion.identity, new Vector3(0.145f, 0.075f, 0.115f), shellMaterial);
        CreateMeshPart(root.transform, "TrackingRingTop", CreateTorusMesh(), new Vector3(0f, 0.105f, 0.065f), Quaternion.Euler(74f, 0f, 0f), new Vector3(0.18f, 0.18f, 0.025f), shellMaterial);
        CreatePrimitivePart(root.transform, "Trigger", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0.102f), Quaternion.Euler(-14f, 0f, 0f), new Vector3(0.05f, 0.09f, 0.025f), darkMaterial);
        CreatePrimitivePart(root.transform, "Thumbstick", PrimitiveType.Cylinder, new Vector3(-0.036f, 0.088f, 0.057f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.026f, 0.011f, 0.026f), darkMaterial);
        CreatePrimitivePart(root.transform, "ButtonA", PrimitiveType.Cylinder, new Vector3(0.036f, 0.087f, 0.06f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.018f, 0.009f, 0.018f), darkMaterial);
        CreatePrimitivePart(root.transform, "ButtonB", PrimitiveType.Cylinder, new Vector3(0.067f, 0.074f, 0.057f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.016f, 0.008f, 0.016f), darkMaterial);
        CreatePrimitivePart(root.transform, "AimGlow", PrimitiveType.Cylinder, new Vector3(0f, 0.022f, 0.128f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.018f, 0.01f, 0.018f), glowMaterial);

        return root;
    }

    private GameObject CreatePrimitivePart(Transform parent, string partName, PrimitiveType primitiveType, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;

        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }

        return part;
    }

    private GameObject CreateMeshPart(Transform parent, string partName, Mesh mesh, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        renderer.material = material;

        return part;
    }

    private Mesh CreateTorusMesh()
    {
        const int majorSegments = 36;
        const int minorSegments = 10;
        const float majorRadius = 0.5f;
        const float minorRadius = 0.08f;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[majorSegments * minorSegments];
        int[] triangles = new int[majorSegments * minorSegments * 6];

        for (int i = 0; i < majorSegments; i++)
        {
            float majorAngle = i * Mathf.PI * 2f / majorSegments;
            Vector3 center = new Vector3(Mathf.Cos(majorAngle) * majorRadius, Mathf.Sin(majorAngle) * majorRadius, 0f);

            for (int j = 0; j < minorSegments; j++)
            {
                float minorAngle = j * Mathf.PI * 2f / minorSegments;
                Vector3 radial = new Vector3(Mathf.Cos(majorAngle), Mathf.Sin(majorAngle), 0f);
                Vector3 normal = radial * Mathf.Cos(minorAngle) + Vector3.forward * Mathf.Sin(minorAngle);
                vertices[i * minorSegments + j] = center + normal * minorRadius;
            }
        }

        int index = 0;
        for (int i = 0; i < majorSegments; i++)
        {
            int nextI = (i + 1) % majorSegments;
            for (int j = 0; j < minorSegments; j++)
            {
                int nextJ = (j + 1) % minorSegments;
                int a = i * minorSegments + j;
                int b = nextI * minorSegments + j;
                int c = nextI * minorSegments + nextJ;
                int d = i * minorSegments + nextJ;

                triangles[index++] = a;
                triangles[index++] = b;
                triangles[index++] = c;
                triangles[index++] = a;
                triangles[index++] = c;
                triangles[index++] = d;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateControllerMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;

        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
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

        MoveWithCollision(movement * moveSpeed * Time.deltaTime);
    }

    private void MoveWithCollision(Vector3 movement)
    {
        if (movement.sqrMagnitude <= 0.000001f)
            return;

        if (locomotionCollider == null)
        {
            locomotionRoot.position += movement;
            return;
        }

        Vector3 direction = movement.normalized;
        float distance = movement.magnitude;

        if (!CapsuleCast(direction, distance + collisionSkin, out RaycastHit hit))
        {
            locomotionRoot.position += movement;
            return;
        }

        float allowedDistance = Mathf.Max(0f, hit.distance - collisionSkin);
        locomotionRoot.position += direction * allowedDistance;

        Vector3 remaining = movement - direction * allowedDistance;
        Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
        slide.y = 0f;

        if (slide.sqrMagnitude > 0.000001f && !CapsuleCast(slide.normalized, slide.magnitude + collisionSkin, out _))
        {
            locomotionRoot.position += slide;
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

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];

            if (candidate.collider == null)
                continue;

            if (candidate.collider == locomotionCollider || candidate.collider.transform.IsChildOf(locomotionRoot))
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

        if (hit.collider.GetComponentInParent<TempBossController>() != null)
            return false;

        if (hit.collider.GetComponentInParent<EnemyHealth>() != null)
            return true;

        GameObject hitObject = hit.collider.gameObject;
        if (hitObject.CompareTag("Ground"))
            return true;

        string objectName = hitObject.name;
        return objectName.Contains("Ground") || objectName.Contains("Floor");
    }

    private void GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius)
    {
        Vector3 center = locomotionRoot.TransformPoint(locomotionCollider.center);
        float height = Mathf.Max(locomotionCollider.height * locomotionRoot.lossyScale.y, 0.01f);
        radius = locomotionCollider.radius * Mathf.Max(locomotionRoot.lossyScale.x, locomotionRoot.lossyScale.z);
        float cylinderHeight = Mathf.Max(0f, height * 0.5f - radius);

        point1 = center + Vector3.up * cylinderHeight;
        point2 = center - Vector3.up * cylinderHeight;
    }
}
