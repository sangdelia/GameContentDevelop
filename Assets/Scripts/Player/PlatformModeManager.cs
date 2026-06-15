using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class PlatformModeManager : MonoBehaviour
{
    private enum EditorModeOverride
    {
        Auto,
        ForcePc,
        ForceVr
    }

    [Header("Editor Test")]
    [SerializeField] private EditorModeOverride editorModeOverride = EditorModeOverride.Auto;

    [Header("PC Objects")]
    [SerializeField] private GameObject pcPlayer;
    [SerializeField] private GameObject pcCamera;
    [SerializeField] private GameObject pcUI;

    [Header("VR Objects")]
    [SerializeField] private GameObject xrOrigin;
    [SerializeField] private GameObject vrLeftController;
    [SerializeField] private GameObject vrRightController;
    [SerializeField] private GameObject vrUI;

    [Header("Shared Components")]
    [SerializeField] private PlayerDummyMove pcMove;
    [SerializeField] private PlayerShootTest playerShoot;
    [SerializeField] private SimpleQuest2VrRig vrRig;
    [SerializeField] private XROrigin xrOriginComponent;
    [SerializeField] private XRInteractionManager xrInteractionManager;
    [SerializeField] private Camera pcCameraComponent;
    [SerializeField] private Camera xrCameraComponent;
    [SerializeField] private bool keepPlayerRootActiveInVr = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeManager()
    {
        if (FindFirstObjectByType<PlatformModeManager>() != null)
            return;

        GameObject managerObject = new GameObject("PlatformModeManager");
        managerObject.AddComponent<PlatformModeManager>();
    }

    private void Awake()
    {
        DiscoverMissingReferences();
        EnsureRuntimeVrRig();
        ApplyCurrentPlatformMode();
    }

    private void Start()
    {
        DiscoverMissingReferences();
        EnsureRuntimeVrRig();
        ApplyCurrentPlatformMode();
    }

    public void ApplyCurrentPlatformMode()
    {
        StargravePlayMode.Mode mode = DetectMode();
        ApplyMode(mode);
    }

    public void ApplyMode(StargravePlayMode.Mode mode)
    {
        DiscoverMissingReferences();
        EnsureRuntimeVrRig();
        StargravePlayMode.SetMode(mode);

        bool useVr = mode == StargravePlayMode.Mode.VrQuest2;
        ApplyPcObjects(!useVr);
        ApplyVrObjects(useVr);
        ConfigurePlayerInput(mode);
        Camera activeCamera = useVr ? xrCameraComponent : pcCameraComponent;
        EnsureSingleMainCamera(activeCamera);
        EnsureSingleAudioListener(activeCamera);
        EnsureSingleEventSystem();
        ConfigureRuntimeUI(mode, activeCamera);
    }

    private StargravePlayMode.Mode DetectMode()
    {
#if UNITY_EDITOR
        if (editorModeOverride == EditorModeOverride.ForcePc)
            return StargravePlayMode.Mode.Pc;

        if (editorModeOverride == EditorModeOverride.ForceVr)
            return StargravePlayMode.Mode.VrQuest2;

        return XRSettings.isDeviceActive ? StargravePlayMode.Mode.VrQuest2 : StargravePlayMode.Mode.Pc;
#elif UNITY_ANDROID
        return StargravePlayMode.Mode.VrQuest2;
#elif UNITY_STANDALONE
        return StargravePlayMode.Mode.Pc;
#else
        return XRSettings.isDeviceActive ? StargravePlayMode.Mode.VrQuest2 : StargravePlayMode.Mode.Pc;
#endif
    }

    private void DiscoverMissingReferences()
    {
        PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();
        if (playerLevel != null && pcPlayer == null)
        {
            pcPlayer = playerLevel.gameObject;
        }

        if (pcMove == null && pcPlayer != null)
        {
            pcMove = pcPlayer.GetComponent<PlayerDummyMove>();
        }

        if (playerShoot == null && pcPlayer != null)
        {
            playerShoot = pcPlayer.GetComponent<PlayerShootTest>();
        }

        if (pcCameraComponent == null)
        {
            pcCameraComponent = Camera.main;
        }

        if (pcCamera == null && pcCameraComponent != null)
        {
            pcCamera = pcCameraComponent.gameObject;
        }
    }

    private void EnsureRuntimeVrRig()
    {
        EnsureXrInteractionManager();

        if (xrOrigin != null && vrRig != null && xrCameraComponent != null && vrLeftController != null && vrRightController != null)
        {
            ConfigureXrOriginComponent();
            vrRig.Configure(GetLocomotionRoot(), xrCameraComponent, vrLeftController.transform, vrRightController.transform);
            return;
        }

        Transform playerRoot = GetLocomotionRoot();
        if (playerRoot == null)
            return;

        if (xrOrigin == null)
        {
            xrOrigin = new GameObject("XR_Origin_Runtime");
            xrOrigin.transform.SetParent(playerRoot, false);
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
        }

        Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
        if (cameraOffset == null)
        {
            GameObject cameraOffsetObject = new GameObject("Camera Offset");
            cameraOffsetObject.transform.SetParent(xrOrigin.transform, false);
            cameraOffsetObject.transform.localPosition = Vector3.zero;
            cameraOffsetObject.transform.localRotation = Quaternion.identity;
            cameraOffset = cameraOffsetObject.transform;
        }

        if (xrCameraComponent == null)
        {
            GameObject cameraObject = new GameObject("XR Camera");
            cameraObject.transform.SetParent(cameraOffset, false);
            cameraObject.transform.localPosition = Vector3.up * 1.6f;
            xrCameraComponent = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }
        else if (xrCameraComponent.transform.parent != cameraOffset)
        {
            xrCameraComponent.transform.SetParent(cameraOffset, true);
        }

        if (vrLeftController == null)
        {
            vrLeftController = new GameObject("VR Left Controller");
            vrLeftController.transform.SetParent(cameraOffset, false);
        }
        else if (vrLeftController.transform.parent != cameraOffset)
        {
            vrLeftController.transform.SetParent(cameraOffset, false);
        }

        if (vrRightController == null)
        {
            vrRightController = new GameObject("VR Right Controller");
            vrRightController.transform.SetParent(cameraOffset, false);
        }
        else if (vrRightController.transform.parent != cameraOffset)
        {
            vrRightController.transform.SetParent(cameraOffset, false);
        }

        if (vrRig == null)
        {
            vrRig = xrOrigin.GetComponent<SimpleQuest2VrRig>();
            if (vrRig == null)
            {
                vrRig = xrOrigin.AddComponent<SimpleQuest2VrRig>();
            }
        }

        ConfigureXrOriginComponent();
        vrRig.Configure(playerRoot, xrCameraComponent, vrLeftController.transform, vrRightController.transform);
    }

    private void EnsureXrInteractionManager()
    {
        if (xrInteractionManager != null)
            return;

        xrInteractionManager = FindFirstObjectByType<XRInteractionManager>();
        if (xrInteractionManager != null)
            return;

        GameObject managerObject = new GameObject("XR Interaction Manager");
        xrInteractionManager = managerObject.AddComponent<XRInteractionManager>();
    }

    private void ConfigureXrOriginComponent()
    {
        if (xrOrigin == null || xrCameraComponent == null)
            return;

        if (xrOriginComponent == null)
        {
            xrOriginComponent = xrOrigin.GetComponent<XROrigin>();
            if (xrOriginComponent == null)
            {
                xrOriginComponent = xrOrigin.AddComponent<XROrigin>();
            }
        }

        Transform cameraOffset = xrCameraComponent.transform.parent != null ? xrCameraComponent.transform.parent : xrOrigin.transform;
        xrOriginComponent.Origin = xrOrigin;
        xrOriginComponent.CameraFloorOffsetObject = cameraOffset.gameObject;
        xrOriginComponent.Camera = xrCameraComponent;
        xrOriginComponent.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        xrOriginComponent.CameraYOffset = 1.6f;
    }

    private Transform GetLocomotionRoot()
    {
        if (pcPlayer != null)
            return pcPlayer.transform;

        PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();
        return playerLevel != null ? playerLevel.transform : null;
    }

    private void ApplyPcObjects(bool active)
    {
        SetActiveSafe(pcCamera, active);
        SetActiveSafe(pcUI, active);

        if (pcPlayer != null)
        {
            pcPlayer.SetActive(active || keepPlayerRootActiveInVr);
        }

        if (pcMove != null)
        {
            pcMove.enabled = active;
        }
    }

    private void ApplyVrObjects(bool active)
    {
        SetActiveSafe(xrOrigin, active);
        SetActiveSafe(vrLeftController, active);
        SetActiveSafe(vrRightController, active);
        SetActiveSafe(vrUI, active);

        if (vrRig != null)
        {
            vrRig.enabled = active;
        }
    }

    private void ConfigurePlayerInput(StargravePlayMode.Mode mode)
    {
        if (playerShoot == null)
            return;

        Camera activeCamera = mode == StargravePlayMode.Mode.VrQuest2 ? xrCameraComponent : pcCameraComponent;
        Transform aimSource = mode == StargravePlayMode.Mode.VrQuest2 && vrRig != null
            ? vrRig.RightController
            : null;

        playerShoot.enabled = true;
        playerShoot.SetRuntimeMode(mode, activeCamera, aimSource);
    }

    private void ConfigureRuntimeUI(StargravePlayMode.Mode mode, Camera activeCamera)
    {
        StargraveRuntimeUI runtimeUI = FindFirstObjectByType<StargraveRuntimeUI>();
        if (runtimeUI != null)
        {
            runtimeUI.ApplyPlatformMode(mode, activeCamera);
        }
    }

    private void EnsureSingleMainCamera(Camera activeCamera)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            bool isActive = camera == activeCamera;
            camera.enabled = isActive;

            if (isActive)
            {
                camera.tag = "MainCamera";
            }
            else if (camera.CompareTag("MainCamera"))
            {
                camera.tag = "Untagged";
            }
        }
    }

    private void EnsureSingleAudioListener(Camera activeCamera)
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            listener.enabled = activeCamera != null && listener.GetComponent<Camera>() == activeCamera;
        }
    }

    private void EnsureSingleEventSystem()
    {
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (systems.Length == 0)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            EnsureInputSystemUiModule(eventSystemObject);
            return;
        }

        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].gameObject.SetActive(i == 0);
        }

        EnsureInputSystemUiModule(systems[0].gameObject);
    }

    private void EnsureInputSystemUiModule(GameObject eventSystemObject)
    {
        InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        if (inputModule.actionsAsset == null)
        {
            inputModule.AssignDefaultActions();
        }
    }

    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
