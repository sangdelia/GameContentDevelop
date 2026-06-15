using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;

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
        if (xrOrigin != null && vrRig != null && xrCameraComponent != null)
            return;

        Transform playerRoot = pcPlayer != null ? pcPlayer.transform : null;
        if (playerRoot == null)
            return;

        if (xrOrigin == null)
        {
            xrOrigin = new GameObject("XR_Origin_Runtime");
            xrOrigin.transform.SetParent(playerRoot, false);
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;
        }

        if (xrCameraComponent == null)
        {
            GameObject cameraObject = new GameObject("XR Camera");
            cameraObject.transform.SetParent(xrOrigin.transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.6f;
            xrCameraComponent = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        if (vrLeftController == null)
        {
            vrLeftController = new GameObject("VR Left Controller");
            vrLeftController.transform.SetParent(xrOrigin.transform, false);
        }

        if (vrRightController == null)
        {
            vrRightController = new GameObject("VR Right Controller");
            vrRightController.transform.SetParent(xrOrigin.transform, false);
        }

        if (vrRig == null)
        {
            vrRig = xrOrigin.GetComponent<SimpleQuest2VrRig>();
            if (vrRig == null)
            {
                vrRig = xrOrigin.AddComponent<SimpleQuest2VrRig>();
            }
        }

        vrRig.Configure(playerRoot, xrCameraComponent, vrLeftController.transform, vrRightController.transform);
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
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            return;
        }

        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].gameObject.SetActive(i == 0);
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
