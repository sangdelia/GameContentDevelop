using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class StargraveRuntimeUI : MonoBehaviour
{
    [Header("PC Test Layout")]
    [SerializeField] private bool useStartScreen = false;
    [SerializeField] private Vector2 canvasSize = new Vector2(1200f, 700f);

    private Canvas canvas;
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

    private PlayerLevel playerLevel;
    private PlayerHealth playerHealth;
    private PlayerShootTest playerShoot;
    private PlayerDummyMove playerMove;
    private GameProgressManager progressManager;
    private EnemyHealth bossHealth;

    private bool isStarted;
    private bool isChoosingTrait;
    private bool isBound;
    private bool playerMoveWasEnabled;
    private bool playerShootWasEnabled;
    private int damageTraitCount;
    private int speedTraitCount;
    private int magnetTraitCount;

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

        if (!useStartScreen)
        {
            ShowGameplay();
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
        EnsureHudVisibleDuringGameplay();

        if (progressManager == null)
        {
            BindProgressManager();
        }

        if (useStartScreen && !isStarted && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            StartGame();
        }

        if (isChoosingTrait && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectTrait(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectTrait(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectTrait(2);
        }
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

            playerLevel.ExpChanged += HandleExpChanged;
            playerLevel.LevelChanged += HandleLevelChanged;
            playerLevel.LevelUpChoicesRequested += ShowTraitChoices;
            playerHealth.HealthChanged += HandleHealthChanged;
            playerHealth.Died += ShowGameOver;

            HandleExpChanged(playerLevel.Level, playerLevel.CurrentExp, playerLevel.RequiredExp);
            HandleLevelChanged(playerLevel.Level);
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
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
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("PcTestCanvas");
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
        CreateText(startPanel.transform, "WASD Move   Mouse Aim   Left Click Fire   Enter Start", 26, TextAnchor.MiddleCenter, new Vector2(0f, 88f), new Vector2(900f, 45f));

        CreateButton(startPanel.transform, "START", new Vector2(0f, -20f), new Vector2(260f, 72f), StartGame);
        CreateButton(startPanel.transform, "QUIT", new Vector2(0f, -112f), new Vector2(260f, 62f), QuitGame);
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
        traitInfoText = CreateAnchoredText(hudPanel.transform, "Traits: none", 21, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-28f, -60f), new Vector2(360f, 36f));
        objectiveText = CreateAnchoredText(hudPanel.transform, "Kills 0 / 10", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(520f, 38f));

        bossNameText = CreateAnchoredText(hudPanel.transform, "STARGRAVE CORE", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(560f, 34f));
        bossHealthFill = CreateAnchoredBar(hudPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(620f, 24f), new Color(0.95f, 0.12f, 0.5f, 1f));
        bossNameText.gameObject.SetActive(false);
        bossHealthFill.transform.parent.gameObject.SetActive(false);

        expText = CreateAnchoredText(hudPanel.transform, "EXP 0 / 10", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(520f, 34f));
        expFill = CreateAnchoredBar(hudPanel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(680f, 26f), new Color(0.2f, 0.75f, 1f, 1f));
    }

    private void BuildTraitPanel()
    {
        traitPanel = CreatePanel("TraitPanel", root, new Color(0.02f, 0.05f, 0.09f, 0.93f));
        CreateText(traitPanel.transform, "SELECT AUGMENT", 44, TextAnchor.MiddleCenter, new Vector2(0f, 210f), new Vector2(780f, 70f));
        CreateText(traitPanel.transform, "Choose one upgrade. Press 1 / 2 / 3.", 24, TextAnchor.MiddleCenter, new Vector2(0f, 154f), new Vector2(780f, 42f));

        CreateButton(traitPanel.transform, "1  OVERCHARGED ROUNDS\nDamage +25%", new Vector2(-310f, -12f), new Vector2(260f, 210f), () => SelectTrait(0));
        CreateButton(traitPanel.transform, "2  COMBAT STIMS\nMove Speed +15%", new Vector2(0f, -12f), new Vector2(260f, 210f), () => SelectTrait(1));
        CreateButton(traitPanel.transform, "3  GRAVITY COLLECTOR\nEXP Pull Range +2m", new Vector2(310f, -12f), new Vector2(260f, 210f), () => SelectTrait(2));
    }

    private void BuildEndPanel()
    {
        endPanel = CreatePanel("EndPanel", root, new Color(0.03f, 0.02f, 0.02f, 0.9f));
        endTitleText = CreateText(endPanel.transform, "MISSION FAILED", 54, TextAnchor.MiddleCenter, new Vector2(0f, 92f), new Vector2(780f, 86f));
        CreateText(endPanel.transform, "Demo flow endpoint", 24, TextAnchor.MiddleCenter, new Vector2(0f, 20f), new Vector2(600f, 42f));
        CreateButton(endPanel.transform, "QUIT", new Vector2(0f, -92f), new Vector2(240f, 62f), QuitGame);
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void StartGame()
    {
        BindPlayer();
        ShowGameplay();
    }

    private void ShowGameplay()
    {
        Time.timeScale = 1f;
        isStarted = true;
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
        traitPanel.SetActive(true);
        SetPlayerControlsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SelectTrait(int index)
    {
        if (!isChoosingTrait)
            return;

        if (index == 0 && playerShoot != null)
        {
            playerShoot.AddDamageMultiplier(1.25f);
            damageTraitCount++;
        }
        else if (index == 1 && playerMove != null)
        {
            playerMove.AddMoveSpeedMultiplier(1.15f);
            speedTraitCount++;
        }
        else if (index == 2)
        {
            ExpOrb.AddGlobalAttractBonus(2f);
            magnetTraitCount++;
        }

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
        endTitleText.text = title;
        startPanel.SetActive(false);
        hudPanel.SetActive(false);
        traitPanel.SetActive(false);
        endPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        traitInfoText.text = $"Traits: DMG x{damageTraitCount}  SPD x{speedTraitCount}  MAG x{magnetTraitCount}";
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (healthText != null)
        {
            healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        if (healthFill != null)
        {
            healthFill.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
