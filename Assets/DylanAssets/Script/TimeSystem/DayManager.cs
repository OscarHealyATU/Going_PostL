using System;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Settings")]
    [SerializeField] private int startHour24 = 9;
    [SerializeField] private int endHour24 = 17;
    [SerializeField] private float realMinutesPerWorkDay = 20f;
    [SerializeField] private string warehouseSceneName = "Warehouse";

    [Header("Player Lock While Summary Open")]
    [SerializeField] private PlayerMovementInside playerMovementInsideScript;
    [SerializeField] private PlayerMovementOutside playerMovementOutsideScript;
    [SerializeField] private PlayerLook playerLookScript;
    [SerializeField] private FlyoverController flyoverLookScript;
    [SerializeField] private bool unlockCursorWhenSummaryOpen = true;

    [Header("New Day Transition")]
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float blackScreenHold = 0.35f;
    [SerializeField] private float dayMessageHold = 1.0f;
    [SerializeField] private string dayMessageFormat = "DAY {0}";

    private CanvasGroup dayTransitionFadeGroup;
    private TMP_Text dayTransitionText;

    private GameObject clockRoot;
    private TMP_Text clockText;
    private TMP_Text dayText;
    private GameObject endDayButtonObject;
    private GameObject daySummaryPanel;
    private TMP_Text summaryTitleText;
    private TMP_Text packagesDeliveredText;
    private TMP_Text moneyEarnedText;
    private TMP_Text moneySpentText;
    private TMP_Text totalRevenueText;
    private TMP_Text experienceEarnedText;

    private Button endDayButton;
    private Button startNextDayButton;
    private Button closeSummaryButton;

    private DayState state;
    private float accumulatedSeconds;
    private bool hasTriggeredEndOfDayRefresh;

    private bool previousInsideMovementEnabled;
    private bool previousOutsideMovementEnabled;
    private bool previousPlayerLookEnabled;
    private bool previousFlyoverLookEnabled;

    private int StartMinute => startHour24 * 60;
    private int EndMinute => endHour24 * 60;
    private int WorkDayMinutes => EndMinute - StartMinute;
    private float RealSecondsPerGameMinute => (realMinutesPerWorkDay * 60f) / Mathf.Max(1, WorkDayMinutes);

    public bool CanSpawnItems => state != null && state.isDayEnded == 0 && state.currentMinuteOfDay < EndMinute;
    public bool IsDayEnded => state != null && state.isDayEnded == 1;
    public int CurrentMinuteOfDay => state != null ? state.currentMinuteOfDay : StartMinute;
    public int CurrentDayNumber => state != null ? state.dayNumber : 1;

    private bool isTransitioningDay;

    private SQLite.SQLiteConnection Db
    {
        get
        {
            if (DbBoot.Instance == null || DbBoot.Instance.Db == null)
            {
                //debug.LogError("[DayManager] DbBoot/Db is missing.");
                return null;
            }

            return DbBoot.Instance.Db;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        CachePlayerControlScripts();
        LoadOrCreateState();
        ClampState();
        SaveState();
        RefreshUIImmediate();

        //debug.Log("[DayManager] Started. Day = " + CurrentDayNumber + ", Time = " + FormatTime(CurrentMinuteOfDay));
    }

    private void Update()
    {
        if (state == null)
            return;

        if (state.isDayEnded == 1)
        {
            if (!hasTriggeredEndOfDayRefresh)
            {
                hasTriggeredEndOfDayRefresh = true;
                RefreshUIImmediate();
            }

            RefreshClockText();
            return;
        }

        if (state.currentMinuteOfDay >= EndMinute)
        {
            EndWorkDayInternal();
            return;
        }

        accumulatedSeconds += Time.deltaTime;

        while (accumulatedSeconds >= RealSecondsPerGameMinute)
        {
            accumulatedSeconds -= RealSecondsPerGameMinute;
            state.currentMinuteOfDay++;

            RefreshClockText();

            if (state.currentMinuteOfDay >= EndMinute)
            {
                state.currentMinuteOfDay = EndMinute;
                EndWorkDayInternal();
                return;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        clockRoot = null;
        clockText = null;
        dayText = null;

        endDayButtonObject = null;
        endDayButton = null;
        startNextDayButton = null;
        closeSummaryButton = null;

        daySummaryPanel = null;
        summaryTitleText = null;
        packagesDeliveredText = null;
        moneyEarnedText = null;
        moneySpentText = null;
        totalRevenueText = null;
        experienceEarnedText = null;

        dayTransitionFadeGroup = null;
        dayTransitionText = null;

        CachePlayerControlScripts();
        RefreshUIImmediate();

        //debug.Log("[DayManager] Scene loaded: " + scene.name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void BindSceneUI(DaySceneUI ui)
    {
        if (ui == null)
            return;

        clockRoot = ui.ClockRoot;
        clockText = ui.ClockText;
        dayText = ui.DayText;

        endDayButtonObject = ui.EndDayButtonObject;
        endDayButton = ui.EndDayButton;
        startNextDayButton = ui.StartNextDayButton;
        closeSummaryButton = ui.CloseSummaryButton;

        daySummaryPanel = ui.DaySummaryPanel;
        summaryTitleText = ui.SummaryTitleText;
        packagesDeliveredText = ui.PackagesDeliveredText;
        moneyEarnedText = ui.MoneyEarnedText;
        moneySpentText = ui.MoneySpentText;
        totalRevenueText = ui.TotalRevenueText;
        experienceEarnedText = ui.ExperienceEarnedText;

        dayTransitionFadeGroup = ui.DayTransitionFadeGroup;
        dayTransitionText = ui.DayTransitionText;

        WireButtons();
        RefreshUIImmediate();

        if (dayTransitionFadeGroup != null)
        {
            if (!isTransitioningDay)
            {
                dayTransitionFadeGroup.alpha = 0f;
                dayTransitionFadeGroup.blocksRaycasts = false;
                dayTransitionFadeGroup.interactable = false;
            }
            else
            {
                dayTransitionFadeGroup.alpha = 1f;
                dayTransitionFadeGroup.blocksRaycasts = true;
                dayTransitionFadeGroup.interactable = true;
            }
        }

        if (dayTransitionText != null)
        {
            if (!isTransitioningDay)
            {
                dayTransitionText.gameObject.SetActive(false);
                dayTransitionText.text = string.Empty;
            }
        }

        //debug.Log("[DayManager] Scene UI bound.");
    }

    private void WireButtons()
    {
        if (endDayButton != null)
        {
            endDayButton.onClick.RemoveListener(OpenDaySummary);
            endDayButton.onClick.AddListener(OpenDaySummary);
        }

        if (startNextDayButton != null)
        {
            startNextDayButton.onClick.RemoveListener(StartNextDay);
            startNextDayButton.onClick.AddListener(StartNextDay);
        }

        if (closeSummaryButton != null)
        {
            closeSummaryButton.onClick.RemoveListener(CloseDaySummary);
            closeSummaryButton.onClick.AddListener(CloseDaySummary);
        }
    }

    private void LoadOrCreateState()
    {
        var db = Db;
        if (db == null) return;

        state = db.Table<DayState>().FirstOrDefault();

        if (state == null)
        {
            state = new DayState
            {
                id = 1,
                dayNumber = 1,
                currentMinuteOfDay = StartMinute,
                isDayEnded = 0,
                packagesDeliveredToday = 0,
                moneyEarnedToday = 0.0,
                moneySpentToday = 0.0,
                totalRevenueToday = 0.0,
                experienceEarnedToday = 0
            };

            db.Insert(state);
            //debug.Log("[DayManager] Created new DayState row.");
        }
    }

    private void ClampState()
    {
        if (state == null) return;

        if (state.currentMinuteOfDay < StartMinute)
            state.currentMinuteOfDay = StartMinute;

        if (state.currentMinuteOfDay >= EndMinute)
        {
            state.currentMinuteOfDay = EndMinute;
            state.isDayEnded = 1;
            hasTriggeredEndOfDayRefresh = false;
        }
    }

    private void SaveState()
    {
        var db = Db;
        if (db == null || state == null) return;

        state.totalRevenueToday = state.moneyEarnedToday - state.moneySpentToday;
        db.Update(state);
    }

    private void EndWorkDayInternal()
    {
        if (state == null) return;

        state.currentMinuteOfDay = EndMinute;
        state.isDayEnded = 1;
        hasTriggeredEndOfDayRefresh = true;

        SaveState();
        RefreshUIImmediate();

        //debug.Log("[DayManager] Work day ended. End Day button should now be visible.");
    }

    public void RegisterDelivery(double moneyEarned, int experienceEarned)
    {
        if (state == null)
            LoadOrCreateState();

        if (state == null) return;

        state.packagesDeliveredToday += 1;
        state.moneyEarnedToday += moneyEarned;
        state.experienceEarnedToday += experienceEarned;
        state.totalRevenueToday = state.moneyEarnedToday - state.moneySpentToday;

        SaveState();
        RefreshSummaryIfOpen();
    }

    public void RegisterMoneyEarned(double amountEarned)
    {
        if (state == null)
            LoadOrCreateState();

        if (state == null) return;

        state.moneyEarnedToday += Math.Abs(amountEarned);
        state.totalRevenueToday = state.moneyEarnedToday - state.moneySpentToday;

        SaveState();
        RefreshSummaryIfOpen();
    }

    public void RegisterMoneySpent(double amountSpent)
    {
        if (state == null)
            LoadOrCreateState();

        if (state == null) return;

        state.moneySpentToday += Math.Abs(amountSpent);
        state.totalRevenueToday = state.moneyEarnedToday - state.moneySpentToday;

        SaveState();
        RefreshSummaryIfOpen();
    }

    public void OpenDaySummary()
    {
        //debug.Log("[DayManager] OpenDaySummary called.");

        if (state == null)
            LoadOrCreateState();

        if (state == null)
        {
            //debug.LogWarning("[DayManager] OpenDaySummary failed: state is null.");
            return;
        }

        if (state.isDayEnded == 0)
        {
            //debug.LogWarning("[DayManager] OpenDaySummary blocked: day has not ended yet.");
            return;
        }

        if (daySummaryPanel == null)
        {
            //debug.LogWarning("[DayManager] OpenDaySummary failed: daySummaryPanel is null.");
            return;
        }

        daySummaryPanel.SetActive(true);
        PopulateSummary();
        SetSummaryMovementLock(true);

        //debug.Log("[DayManager] Summary panel opened.");
    }

    public void CloseDaySummary()
    {
        if (daySummaryPanel != null)
        {
            daySummaryPanel.SetActive(false);
            SetSummaryMovementLock(false);
            //debug.Log("[DayManager] Summary panel closed.");
        }
    }

    public void StartNextDay()
    {
        if (isTransitioningDay)
            return;

        //debug.Log("[DayManager] StartNextDay called.");

        if (state == null)
            LoadOrCreateState();

        if (state == null)
        {
            //debug.LogWarning("[DayManager] StartNextDay failed: state is null.");
            return;
        }

        StartCoroutine(StartNextDayRoutine());
    }

    private IEnumerator StartNextDayRoutine()
    {
        isTransitioningDay = true;

        if (startNextDayButton != null)
            startNextDayButton.interactable = false;

        if (closeSummaryButton != null)
            closeSummaryButton.interactable = false;

        int nextDayNumber = state.dayNumber + 1;

        yield return FadeCanvasGroup(dayTransitionFadeGroup, 0f, 1f, fadeOutDuration);
        yield return new WaitForSecondsRealtime(blackScreenHold);

        BeginNextDayStateOnly();

        SceneManager.LoadScene(warehouseSceneName);

        while (dayTransitionFadeGroup == null || dayTransitionText == null)
            yield return null;

        dayTransitionText.text = string.Format(dayMessageFormat, nextDayNumber);
        dayTransitionText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(dayMessageHold);

        if (dayTransitionText != null)
        {
            dayTransitionText.gameObject.SetActive(false);
            dayTransitionText.text = string.Empty;
        }

        yield return FadeCanvasGroup(dayTransitionFadeGroup, 1f, 0f, fadeInDuration);

        if (dayTransitionFadeGroup != null)
        {
            dayTransitionFadeGroup.blocksRaycasts = false;
            dayTransitionFadeGroup.interactable = false;
        }

        isTransitioningDay = false;
    }

    private void BeginNextDayStateOnly()
    {
        state.dayNumber += 1;
        state.currentMinuteOfDay = StartMinute;
        state.isDayEnded = 0;
        state.packagesDeliveredToday = 0;
        state.moneyEarnedToday = 0.0;
        state.moneySpentToday = 0.0;
        state.totalRevenueToday = 0.0;
        state.experienceEarnedToday = 0;

        accumulatedSeconds = 0f;
        hasTriggeredEndOfDayRefresh = false;

        SaveState();
        CloseDaySummary();

        //debug.Log("[DayManager] Starting Day " + state.dayNumber + ". Loading scene: " + warehouseSceneName);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        group.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        group.interactable = true;
        group.alpha = from;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    public void ForceEndDayNow()
    {
        //debug.Log("[DayManager] ForceEndDayNow called.");
        EndWorkDayInternal();
    }

    private void RefreshUIImmediate()
    {
        RefreshClockText();

        if (dayText != null)
            dayText.text = $"Day {CurrentDayNumber}";

        if (endDayButtonObject != null)
            endDayButtonObject.SetActive(IsDayEnded);

        if (!IsDayEnded && daySummaryPanel != null && daySummaryPanel.activeSelf)
        {
            daySummaryPanel.SetActive(false);
            SetSummaryMovementLock(false);
        }

        RefreshSummaryIfOpen();
    }

    private void RefreshClockText()
    {
        if (clockText != null)
            clockText.text = FormatTime(CurrentMinuteOfDay);
    }

    private void RefreshSummaryIfOpen()
    {
        if (daySummaryPanel != null && daySummaryPanel.activeSelf)
            PopulateSummary();
    }

    private void PopulateSummary()
    {
        if (state == null) return;

        state.totalRevenueToday = state.moneyEarnedToday - state.moneySpentToday;

        if (summaryTitleText != null)
            summaryTitleText.text = $"Day {state.dayNumber} Summary";

        if (packagesDeliveredText != null)
            packagesDeliveredText.text = $"Packages Delivered: {state.packagesDeliveredToday}";

        if (moneyEarnedText != null)
            moneyEarnedText.text = $"Money Earned: €{state.moneyEarnedToday:0.00}";

        if (moneySpentText != null)
            moneySpentText.text = $"Money Spent: €{state.moneySpentToday:0.00}";

        if (totalRevenueText != null)
            totalRevenueText.text = $"Total Revenue: €{state.totalRevenueToday:0.00}";

        if (experienceEarnedText != null)
            experienceEarnedText.text = $"Experience Earned: {state.experienceEarnedToday}";
    }

    private void SetSummaryMovementLock(bool locked)
    {
        if (playerMovementInsideScript != null)
        {
            if (locked)
            {
                previousInsideMovementEnabled = playerMovementInsideScript.enabled;
                playerMovementInsideScript.enabled = false;
            }
            else
            {
                playerMovementInsideScript.enabled = previousInsideMovementEnabled;
            }
        }

        if (playerMovementOutsideScript != null)
        {
            if (locked)
            {
                previousOutsideMovementEnabled = playerMovementOutsideScript.enabled;
                playerMovementOutsideScript.enabled = false;
            }
            else
            {
                playerMovementOutsideScript.enabled = previousOutsideMovementEnabled;
            }
        }

        if (playerLookScript != null)
        {
            if (locked)
            {
                previousPlayerLookEnabled = playerLookScript.enabled;
                playerLookScript.enabled = false;
            }
            else
            {
                playerLookScript.enabled = previousPlayerLookEnabled;
            }
        }

        if (flyoverLookScript != null)
        {
            if (locked)
            {
                previousFlyoverLookEnabled = flyoverLookScript.enabled;
                flyoverLookScript.enabled = false;
            }
            else
            {
                flyoverLookScript.enabled = previousFlyoverLookEnabled;
            }
        }

        if (unlockCursorWhenSummaryOpen)
        {
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }

    private void CachePlayerControlScripts()
    {
        if (playerMovementInsideScript == null)
            playerMovementInsideScript = FindFirstObjectByType<PlayerMovementInside>();

        if (playerMovementOutsideScript == null)
            playerMovementOutsideScript = FindFirstObjectByType<PlayerMovementOutside>();

        if (playerLookScript == null)
            playerLookScript = FindFirstObjectByType<PlayerLook>();

        if (flyoverLookScript == null)
            flyoverLookScript = FindFirstObjectByType<FlyoverController>();
    }

    private string FormatTime(int minuteOfDay)
    {
        int hour24 = Mathf.Clamp(minuteOfDay / 60, 0, 23);
        int minute = Mathf.Clamp(minuteOfDay % 60, 0, 59);

        string suffix = hour24 >= 12 ? "PM" : "AM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{hour12:00}:{minute:00} {suffix}";
    }
}