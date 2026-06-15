using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StargraveRuntimeUI : MonoBehaviour
{
    [Header("PC Test Layout")]
    [SerializeField] private bool useStartScreen = true;
    [SerializeField] private bool autoStartOnBuildPlatform = true;
    [SerializeField] private Vector2 canvasSize = new Vector2(1200f, 700f);
    [SerializeField] private Vector3 vrCanvasLocalPosition = new Vector3(0f, -0.08f, 1.9f);
    [SerializeField] private float vrCanvasScale = 0.0015f;

    private Canvas canvas;
    private GameObject canvasObject;
    private RectTransform root;
    private Font font;

    private GameObject startPanel;
    private GameObject hudPanel;
    private GameObject traitPanel;
    private GameObject endPanel;

    private Text healthText;
    private Text levelText;
    private Text traitInfoText;
    private Text objectiveText;
    private Text bossNameText;
    private Text expText;
    private Text endTitleText;
    private Image healthFill;
    private Image expFill;
    private Image bossHealthFill;
    private Image damageOverlay;

    private PlayerLevel playerLevel;
    private PlayerHealth playerHealth;
    private PlayerShootTest playerShoot;
    private PlayerDummyMove playerMove;
    private PlayerTraitController playerTraits;
    private GameProgressManager progressManager;
    private EnemyHealth bossHealth;
    private readonly List<TraitChoiceView> currentTraitChoices = new List<TraitChoiceView>();
    private Button[] traitButtons;
    private Button retryButton;

    private bool isStarted;
    private bool isChoosingTrait;
    private bool isEnded;
    private bool isBound;
    private bool playerMoveWasEnabled;
    private bool playerShootWasEnabled;
    private bool startMenuControlsWereStored;
    private float damageFlashTimer;
    private float lastHealthRatio = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeUI()
    {
        if (FindFirstObjectByType<StargraveRuntimeUI>() != null)
            return;

        GameObject uiObject = new GameObject("StargraveRuntimeUI");
        uiObject.AddComponent<StargraveRuntimeUI>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildCanvas();
        ApplyPlatformMode(StargravePlayMode.Current, Camera.main);
        BuildStartPanel();
        BuildHudPanel();
        BuildTraitPanel();
        BuildEndPanel();

        if (useStartScreen)
        {
            ShowStart();
        }
        else
        {
            ShowGameplay();
        }
    }

    private void Start()
    {
        BindPlayer();

        if (autoStartOnBuildPlatform && useStartScreen && ShouldAutoStartForCurrentPlayer())
        {
            StartGame(GetBuildDefaultMode());
            return;
        }

        if (!useStartScreen)
        {
            ShowGameplay();
            return;
        }

        if (!isStarted)
        {
            ShowStart();
        }
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
        {
            playerLevel.ExpChanged -= HandleExpChanged;
            playerLevel.LevelChanged -= HandleLevelChanged;
            playerLevel.LevelUpChoicesRequested -= ShowTraitChoices;
        }

        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            playerHealth.Damaged -= HandlePlayerDamaged;
            playerHealth.Died -= ShowGameOver;
        }

        if (progressManager != null)
        {
            progressManager.ProgressChanged -= HandleProgressChanged;
            progressManager.BossFightStartedEvent -= HandleBossFightStarted;
            progressManager.BossSpawned -= BindBossHealth;
            progressManager.BossDefeated -= ShowClear;
        }

        UnbindBossHealth();
    }

    private void Update()
    {
        if (isEnded)
        {
            KeepEndMenuInteractive();
            UpdateDamageOverlay();
            return;
        }

        EnsureHudVisibleDuringGameplay();

        if (progressManager == null)
        {
            BindProgressManager();
        }

        if (useStartScreen && !isStarted && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            StartPcGame();
        }

        if (useStartScreen && !isStarted)
        {
            KeepStartMenuInteractive();
        }

        if (isChoosingTrait && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectTrait(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectTrait(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectTrait(2);
        }

        UpdateDamageOverlay();
        UpdateLowHealthPulse();
    }

    private void BindPlayer()
    {
        if (isBound)
            return;

        playerLevel = FindFirstObjectByType<PlayerLevel>();
        BindProgressManager();

        if (playerLevel != null)
        {
            playerHealth = playerLevel.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = playerLevel.gameObject.AddComponent<PlayerHealth>();
            }

            playerShoot = playerLevel.GetComponent<PlayerShootTest>();
            playerMove = playerLevel.GetComponent<PlayerDummyMove>();
            playerTraits = playerLevel.GetComponent<PlayerTraitController>();
            if (playerTraits == null)
            {
                playerTraits = playerLevel.gameObject.AddComponent<PlayerTraitController>();
            }

            playerLevel.ExpChanged += HandleExpChanged;
            playerLevel.LevelChanged += HandleLevelChanged;
            playerLevel.LevelUpChoicesRequested += ShowTraitChoices;
            playerHealth.HealthChanged += HandleHealthChanged;
            playerHealth.Damaged += HandlePlayerDamaged;
            playerHealth.Died += ShowGameOver;

            HandleExpChanged(playerLevel.Level, playerLevel.CurrentExp, playerLevel.RequiredExp);
            HandleLevelChanged(playerLevel.Level);
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            UpdateTraitInfoText();
            isBound = true;
        }
    }

    private void BindProgressManager()
    {
        if (progressManager != null)
            return;

        progressManager = FindFirstObjectByType<GameProgressManager>();

        if (progressManager == null)
            return;

        progressManager.ProgressChanged += HandleProgressChanged;
        progressManager.BossFightStartedEvent += HandleBossFightStarted;
        progressManager.BossSpawned += BindBossHealth;
        progressManager.BossDefeated += ShowClear;
        HandleProgressChanged(progressManager.KillCount, progressManager.KillsRequiredForPortal, progressManager.PortalOpened);
    }

    private void EnsureEventSystem()
    {
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems.Length > 0)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].gameObject.SetActive(i == 0);
            }

            EventSystem.current = systems[0];
            EnsureInputSystemUiModule(systems[0].gameObject);
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        EventSystem createdEventSystem = eventSystemObject.AddComponent<EventSystem>();
        EventSystem.current = createdEventSystem;
        EnsureInputSystemUiModule(eventSystemObject);
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

    private void BuildCanvas()
    {
        canvasObject = new GameObject("StargraveRuntimeCanvas");
        canvasObject.transform.SetParent(transform);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = canvasSize;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        root = canvas.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.sizeDelta = Vector2.zero;
    }

    public void ApplyPlatformMode(StargravePlayMode.Mode mode, Camera activeCamera)
    {
        if (canvas == null || canvasObject == null)
            return;

        if (mode == StargravePlayMode.Mode.VrQuest2)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = activeCamera;
            canvasObject.name = "VrWorldSpaceCanvas";

            Transform parent = activeCamera != null ? activeCamera.transform : transform;
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = vrCanvasLocalPosition;
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * vrCanvasScale;

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = canvasSize;
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvasObject.name = "PcScreenCanvas";
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = Vector3.zero;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one;

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.sizeDelta = Vector2.zero;
    }

    private void EnsureHudVisibleDuringGameplay()
    {
        if (!isStarted || isChoosingTrait)
            return;

        if (canvas != null)
        {
            canvas.enabled = true;
        }

        if (hudPanel != null && !hudPanel.activeSelf && (endPanel == null || !endPanel.activeSelf))
        {
            hudPanel.SetActive(true);
        }
    }

    private void BuildStartPanel()
    {
        startPanel = CreatePanel("StartPanel", root, new Color(0.02f, 0.04f, 0.06f, 0.86f));
        CreateText(startPanel.transform, "STARGRAVE SURVIVOR", 58, TextAnchor.MiddleCenter, new Vector2(0f, 185f), new Vector2(900f, 90f));
        CreateText(startPanel.transform, "Select test mode. PC is active now, Quest 2 is prepared for XR rig setup.", 24, TextAnchor.MiddleCenter, new Vector2(0f, 98f), new Vector2(900f, 55f));
        CreateText(startPanel.transform, "PC: WASD Move   Mouse Aim   Left Click Fire   Enter", 22, TextAnchor.MiddleCenter, new Vector2(0f, 52f), new Vector2(900f, 42f));

        CreateButton(startPanel.transform, "PC TEST", new Vector2(-150f, -42f), new Vector2(260f, 72f), StartPcGame);
        CreateButton(startPanel.transform, "VR QUEST 2", new Vector2(150f, -42f), new Vector2(260f, 72f), StartVrGame);
        CreateButton(startPanel.transform, "QUIT", new Vector2(0f, -132f), new Vector2(260f, 62f), QuitGame);
    }

    private void BuildHudPanel()
    {
        hudPanel = new GameObject("HudPanel");
        RectTransform hud = hudPanel.AddComponent<RectTransform>();
        hud.SetParent(root, false);
        hud.anchorMin = Vector2.zero;
        hud.anchorMax = Vector2.one;
        hud.offsetMin = Vector2.zero;
        hud.offsetMax = Vector2.zero;

        healthText = CreateAnchoredText(hudPanel.transform, "HP 100 / 100", 26, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(280f, 38f));
        healthFill = CreateAnchoredBar(hudPanel.transform, new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(280f, 24f), new Color(0.9f, 0.1f, 0.16f, 1f));

        levelText = CreateAnchoredText(hudPanel.transform, "LV 1", 28, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(260f, 38f));
        traitInfoText = CreateAnchoredText(hudPanel.transform, "능력: 없음", 18, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-28f, -60f), new Vector2(470f, 76f));
        objectiveText = CreateAnchoredText(hudPanel.transform, "Kills 0 / 10", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(520f, 38f));

        bossNameText = CreateAnchoredText(hudPanel.transform, "STARGRAVE CORE", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(560f, 34f));
        bossHealthFill = CreateAnchoredBar(hudPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(620f, 24f), new Color(0.95f, 0.12f, 0.5f, 1f));
        bossNameText.gameObject.SetActive(false);
        bossHealthFill.transform.parent.gameObject.SetActive(false);

        expText = CreateAnchoredText(hudPanel.transform, "EXP 0 / 10", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(520f, 34f));
        expFill = CreateAnchoredBar(hudPanel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(680f, 26f), new Color(0.2f, 0.75f, 1f, 1f));
        damageOverlay = CreateScreenOverlay(hudPanel.transform, new Color(1f, 0f, 0f, 0f));
    }

    private void BuildTraitPanel()
    {
        traitPanel = CreatePanel("TraitPanel", root, new Color(0.02f, 0.05f, 0.09f, 0.93f));
        CreateText(traitPanel.transform, "능력 선택", 44, TextAnchor.MiddleCenter, new Vector2(0f, 210f), new Vector2(780f, 70f));
        CreateText(traitPanel.transform, "강화 하나를 선택하세요. 1 / 2 / 3 키도 사용할 수 있습니다.", 24, TextAnchor.MiddleCenter, new Vector2(0f, 154f), new Vector2(780f, 42f));

        traitButtons = new Button[3];
        traitButtons[0] = CreateButton(traitPanel.transform, "1", new Vector2(-310f, -12f), new Vector2(260f, 210f), () => SelectTrait(0));
        traitButtons[1] = CreateButton(traitPanel.transform, "2", new Vector2(0f, -12f), new Vector2(260f, 210f), () => SelectTrait(1));
        traitButtons[2] = CreateButton(traitPanel.transform, "3", new Vector2(310f, -12f), new Vector2(260f, 210f), () => SelectTrait(2));
    }

    private void BuildEndPanel()
    {
        endPanel = CreatePanel("EndPanel", root, new Color(0.03f, 0.02f, 0.02f, 0.9f));
        endTitleText = CreateText(endPanel.transform, "MISSION FAILED", 54, TextAnchor.MiddleCenter, new Vector2(0f, 92f), new Vector2(780f, 86f));
        CreateText(endPanel.transform, "Demo flow endpoint", 24, TextAnchor.MiddleCenter, new Vector2(0f, 20f), new Vector2(600f, 42f));
        retryButton = CreateButton(endPanel.transform, "RETRY", new Vector2(0f, -78f), new Vector2(280f, 66f), RestartGame);
        CreateButton(endPanel.transform, "QUIT", new Vector2(0f, -154f), new Vector2(280f, 62f), QuitGame);
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetCenterAnchor(rect);
        rect.sizeDelta = new Vector2(940f, 560f);
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private Text CreateText(Transform parent, string value, int size, TextAnchor anchor, Vector2 position, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject("Text");
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetCenterAnchor(rect);
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private Text CreateAnchoredText(Transform parent, string value, int size, TextAnchor textAnchor, Vector2 anchor, Vector2 position, Vector2 sizeDelta)
    {
        Text text = CreateText(parent, value, size, textAnchor, position, sizeDelta);
        RectTransform rect = text.GetComponent<RectTransform>();
        SetAnchor(rect, anchor);
        rect.anchoredPosition = position;
        return text;
    }

    private Image CreateBar(Transform parent, Vector2 position, Vector2 sizeDelta, Color fillColor)
    {
        GameObject frameObject = new GameObject("BarFrame");
        RectTransform frameRect = frameObject.AddComponent<RectTransform>();
        frameRect.SetParent(parent, false);
        SetCenterAnchor(frameRect);
        frameRect.anchoredPosition = position;
        frameRect.sizeDelta = sizeDelta;

        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject fillObject = new GameObject("BarFill");
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.SetParent(frameRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        return fillImage;
    }

    private Image CreateAnchoredBar(Transform parent, Vector2 anchor, Vector2 position, Vector2 sizeDelta, Color fillColor)
    {
        Image image = CreateBar(parent, position, sizeDelta, fillColor);
        RectTransform frameRect = image.transform.parent.GetComponent<RectTransform>();
        SetAnchor(frameRect, anchor);
        frameRect.anchoredPosition = position;
        return image;
    }

    private Image CreateScreenOverlay(Transform parent, Color color)
    {
        GameObject overlayObject = new GameObject("DamageOverlay");
        RectTransform rect = overlayObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlayObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        overlayObject.transform.SetAsLastSibling();
        return image;
    }

    private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 sizeDelta, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Button");
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetCenterAnchor(rect);
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.24f, 0.94f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText(buttonObject.transform, label, 24, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta - new Vector2(18f, 18f));
        text.color = new Color(0.9f, 0.98f, 1f, 1f);

        return button;
    }

    private void SetCenterAnchor(RectTransform rect)
    {
        SetAnchor(rect, new Vector2(0.5f, 0.5f));
    }

    private void SetAnchor(RectTransform rect, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
    }

    private void ShowStart()
    {
        Time.timeScale = 0f;
        isStarted = false;
        isChoosingTrait = false;
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        traitPanel.SetActive(false);
        endPanel.SetActive(false);
        KeepStartMenuInteractive();
    }

    private void StartPcGame()
    {
        StartGame(StargravePlayMode.Mode.Pc);
    }

    private void StartVrGame()
    {
        StartGame(StargravePlayMode.Mode.VrQuest2);
    }

    private void StartGame(StargravePlayMode.Mode mode)
    {
        StargravePlayMode.SetMode(mode);
        BindPlayer();
        PlatformModeManager modeManager = FindFirstObjectByType<PlatformModeManager>();
        if (modeManager != null)
        {
            modeManager.ApplyMode(mode);
        }
        else
        {
            ApplyPlatformMode(mode, Camera.main);
        }

        ApplySelectedPlayMode();
        ShowGameplay();
    }

    private void KeepStartMenuInteractive()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();

        if (!isBound)
        {
            BindPlayer();
        }

        if (!startMenuControlsWereStored)
        {
            playerMoveWasEnabled = playerMove != null && playerMove.enabled;
            playerShootWasEnabled = playerShoot != null && playerShoot.enabled;
            startMenuControlsWereStored = true;
        }

        if (playerMove != null)
        {
            playerMove.enabled = false;
        }

        if (playerShoot != null)
        {
            playerShoot.enabled = false;
        }
    }

    private StargravePlayMode.Mode GetBuildDefaultMode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return StargravePlayMode.Mode.VrQuest2;
#elif UNITY_STANDALONE && !UNITY_EDITOR
        return StargravePlayMode.Mode.Pc;
#else
        return StargravePlayMode.Current;
#endif
    }

    private bool ShouldAutoStartForCurrentPlayer()
    {
#if !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    private void ApplySelectedPlayMode()
    {
        if (StargravePlayMode.IsVr)
        {
            Debug.Log("[Stargrave] VR Quest 2 mode selected. Current scene uses the PC dummy player until an XR Origin rig is assigned.");
        }

        if (playerShoot != null)
        {
            playerShoot.enabled = true;
        }

        if (playerMove != null)
        {
            playerMove.enabled = true;
        }
    }

    private void ShowGameplay()
    {
        Time.timeScale = 1f;
        isStarted = true;
        startMenuControlsWereStored = false;
        isEnded = false;
        startPanel.SetActive(false);
        hudPanel.SetActive(true);
        traitPanel.SetActive(false);
        endPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowTraitChoices(int level)
    {
        Time.timeScale = 0f;
        isChoosingTrait = true;
        isEnded = false;
        RollTraitChoices();
        traitPanel.SetActive(true);
        SetPlayerControlsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SelectTrait(int index)
    {
        if (!isChoosingTrait)
            return;

        if (index < 0 || index >= currentTraitChoices.Count)
            return;

        ApplyTrait(currentTraitChoices[index]);

        UpdateTraitInfoText();

        isChoosingTrait = false;
        traitPanel.SetActive(false);
        Time.timeScale = 1f;
        SetPlayerControlsEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (playerMove != null)
        {
            if (!enabled)
            {
                playerMoveWasEnabled = playerMove.enabled;
            }

            playerMove.enabled = enabled && playerMoveWasEnabled;
        }

        if (playerShoot != null)
        {
            if (!enabled)
            {
                playerShootWasEnabled = playerShoot.enabled;
            }

            playerShoot.enabled = enabled && playerShootWasEnabled;
        }
    }

    private void ShowGameOver()
    {
        ShowEnd("MISSION FAILED");
    }

    public void ShowClear()
    {
        ShowEnd("MISSION CLEAR");
    }

    private void ShowEnd(string title)
    {
        Time.timeScale = 0f;
        isEnded = true;
        isChoosingTrait = false;
        endTitleText.text = title;
        startPanel.SetActive(false);
        hudPanel.SetActive(false);
        traitPanel.SetActive(false);
        endPanel.SetActive(true);
        endPanel.transform.SetAsLastSibling();
        SetPlayerControlsEnabled(false);
        KeepEndMenuInteractive();
    }

    private void KeepEndMenuInteractive()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();

        if (canvas != null)
        {
            canvas.enabled = true;
        }

        if (endPanel != null)
        {
            endPanel.SetActive(true);
            endPanel.transform.SetAsLastSibling();
        }

        if (playerMove != null)
        {
            playerMove.enabled = false;
        }

        if (playerShoot != null)
        {
            playerShoot.enabled = false;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && retryButton != null)
        {
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }
    }

    private void HandleExpChanged(int level, int currentExp, int requiredExp)
    {
        if (expText != null)
        {
            expText.text = $"EXP {currentExp} / {requiredExp}";
        }

        if (expFill != null)
        {
            expFill.fillAmount = requiredExp <= 0 ? 0f : Mathf.Clamp01((float)currentExp / requiredExp);
        }
    }

    private void HandleLevelChanged(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"LV {level}";
        }
    }

    private void HandleProgressChanged(int killCount, int requiredKills, bool portalOpened)
    {
        if (objectiveText == null)
            return;

        objectiveText.text = portalOpened
            ? "BOSS PORTAL OPEN - ENTER THE RIFT"
            : $"Kills {killCount} / {requiredKills}";
        objectiveText.color = portalOpened ? new Color(0.2f, 1f, 1f, 1f) : Color.white;
    }

    private void HandleBossFightStarted()
    {
        if (objectiveText != null)
        {
            objectiveText.text = "BOSS FIGHT";
        }
    }

    private void BindBossHealth(EnemyHealth newBossHealth)
    {
        UnbindBossHealth();

        bossHealth = newBossHealth;

        if (bossHealth == null)
            return;

        bossHealth.HealthChanged += HandleBossHealthChanged;
        bossHealth.Died += HandleBossDied;

        if (bossNameText != null)
        {
            bossNameText.gameObject.SetActive(true);
        }

        if (bossHealthFill != null)
        {
            bossHealthFill.transform.parent.gameObject.SetActive(true);
        }

        HandleBossHealthChanged(bossHealth.CurrentHp, bossHealth.MaxHp);
    }

    private void UnbindBossHealth()
    {
        if (bossHealth == null)
            return;

        bossHealth.HealthChanged -= HandleBossHealthChanged;
        bossHealth.Died -= HandleBossDied;
        bossHealth = null;
    }

    private void HandleBossHealthChanged(float current, float max)
    {
        if (bossHealthFill != null)
        {
            bossHealthFill.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }
    }

    private void HandleBossDied(EnemyHealth deadBoss)
    {
        if (bossNameText != null)
        {
            bossNameText.gameObject.SetActive(false);
        }

        if (bossHealthFill != null)
        {
            bossHealthFill.transform.parent.gameObject.SetActive(false);
        }

        UnbindBossHealth();
    }

    private void UpdateTraitInfoText()
    {
        if (traitInfoText == null)
            return;

        traitInfoText.text = playerTraits != null ? playerTraits.GetSummary() : "능력: 없음";
    }

    private void RollTraitChoices()
    {
        currentTraitChoices.Clear();

        if (playerTraits == null && playerLevel != null)
        {
            playerTraits = playerLevel.GetComponent<PlayerTraitController>();
        }

        List<TraitChoiceView> choices = playerTraits != null
            ? playerTraits.RollChoices()
            : new List<TraitChoiceView>();

        for (int i = 0; i < traitButtons.Length; i++)
        {
            if (i >= choices.Count)
            {
                traitButtons[i].gameObject.SetActive(false);
                continue;
            }

            TraitChoiceView choice = choices[i];
            currentTraitChoices.Add(choice);
            SetTraitButton(i, choice);
        }
    }

    private void SetTraitButton(int index, TraitChoiceView choice)
    {
        if (traitButtons == null || index < 0 || index >= traitButtons.Length)
            return;

        traitButtons[index].gameObject.SetActive(true);

        Text buttonText = traitButtons[index].GetComponentInChildren<Text>();

        if (buttonText == null)
            return;

        TraitData trait = choice.Trait;
        string title = trait != null && playerTraits != null ? playerTraits.GetDisplayName(trait) : "알 수 없는 능력";
        string description = playerTraits != null ? playerTraits.GetChoiceDescription(trait) : string.Empty;
        int maxLevel = trait != null ? trait.maxLevel : 1;
        buttonText.text = $"{index + 1}  {title}\n{description}\n등급 {choice.NextLevel} / {maxLevel}";
    }

    private void ApplyTrait(TraitChoiceView choice)
    {
        if (playerTraits != null)
        {
            playerTraits.ApplyTrait(choice.Trait);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (healthText != null)
        {
            string shieldText = playerHealth != null && playerHealth.HasShieldReady ? "  SHIELD" : string.Empty;
            healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}{shieldText}";
        }

        if (healthFill != null)
        {
            lastHealthRatio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            healthFill.fillAmount = lastHealthRatio;
        }
    }

    private void HandlePlayerDamaged(float damage)
    {
        damageFlashTimer = 0.32f;
    }

    private void UpdateDamageOverlay()
    {
        if (damageOverlay == null)
            return;

        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.unscaledDeltaTime;
        }

        float alpha = Mathf.Clamp01(damageFlashTimer / 0.32f) * 0.34f;
        damageOverlay.color = new Color(1f, 0.05f, 0.02f, alpha);
    }

    private void UpdateLowHealthPulse()
    {
        if (healthFill == null)
            return;

        if (lastHealthRatio > 0f && lastHealthRatio <= 0.3f)
        {
            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 8f) * 0.5f;
            healthFill.color = Color.Lerp(new Color(0.9f, 0.1f, 0.16f, 1f), new Color(1f, 0.7f, 0.08f, 1f), pulse);
        }
        else
        {
            healthFill.color = new Color(0.9f, 0.1f, 0.16f, 1f);
        }
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        isEnded = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }
}
