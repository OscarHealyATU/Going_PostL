using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("Animation")]
    [SerializeField] private float fillSpeed = 800f;
    [SerializeField] private float levelUpPause = 0.15f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Level Text Pop")]
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private float popDuration = 0.18f;

    [Header("Optional Level Up Popup")]
    [SerializeField] private GameObject levelUpPopup;
    [SerializeField] private TMP_Text levelUpPopupText;
    [SerializeField] private float popupDuration = 1f;

    private Coroutine animateRoutine;
    private Coroutine levelPopRoutine;
    private Coroutine popupRoutine;

    private int displayedLevel = 1;
    private float displayedExp = 0f;

    private Vector3 levelTextBaseScale = Vector3.one;

    private void Awake()
    {
        if (levelText != null)
            levelTextBaseScale = levelText.rectTransform.localScale;

        if (levelUpPopup != null)
            levelUpPopup.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerService.OnExperienceChangedDetailed += HandleExperienceChangedDetailed;
        RefreshImmediateFromPlayer();
    }

    private void OnDisable()
    {
        PlayerService.OnExperienceChangedDetailed -= HandleExperienceChangedDetailed;
    }

    private void RefreshImmediateFromPlayer()
    {
        int level = PlayerService.GetLevel();
        int currentExp = PlayerService.GetExperienceIntoCurrentLevel();
        int expNeeded = PlayerService.GetExperienceNeededForNextLevel();

        displayedLevel = level;
        displayedExp = currentExp;

        ApplyImmediate(level, currentExp, expNeeded);
    }

    private void HandleExperienceChangedDetailed(
        int oldLevel,
        int oldCurrentExp,
        int oldExpNeeded,
        int newLevel,
        int newCurrentExp,
        int newExpNeeded)
    {
        if (animateRoutine != null)
            StopCoroutine(animateRoutine);

        displayedLevel = oldLevel;
        displayedExp = oldCurrentExp;

        SetSliderInstant(oldCurrentExp, oldExpNeeded);
        UpdateTexts(oldLevel, oldCurrentExp, oldExpNeeded);

        animateRoutine = StartCoroutine(
            AnimateExperienceChangeDetailed(
                oldLevel,
                oldCurrentExp,
                oldExpNeeded,
                newLevel,
                newCurrentExp,
                newExpNeeded
            )
        );
    }

    private IEnumerator AnimateExperienceChangeDetailed(
        int startLevel,
        int startExp,
        int startExpNeeded,
        int targetLevel,
        int targetExp,
        int targetExpNeeded)
    {
        displayedLevel = startLevel;
        displayedExp = startExp;

        if (targetLevel == startLevel)
        {
            yield return AnimateSlider(startExp, targetExp, targetExpNeeded);

            displayedLevel = targetLevel;
            displayedExp = targetExp;
            UpdateTexts(targetLevel, Mathf.RoundToInt(displayedExp), targetExpNeeded);

            animateRoutine = null;
            yield break;
        }

        int currentLevel = startLevel;
        float currentExp = startExp;
        int currentExpNeeded = startExpNeeded;

        while (currentLevel < targetLevel)
        {
            yield return AnimateSlider(currentExp, currentExpNeeded, currentExpNeeded);

            displayedLevel = currentLevel;
            displayedExp = currentExpNeeded;
            UpdateTexts(currentLevel, currentExpNeeded, currentExpNeeded);

            yield return Wait(levelUpPause);

            currentLevel++;
            currentExp = 0f;
            currentExpNeeded = PlayerService.ExpPerLevel;

            displayedLevel = currentLevel;
            displayedExp = 0f;

            SetSliderInstant(0f, currentExpNeeded);
            UpdateTexts(currentLevel, 0, currentExpNeeded);

            PlayLevelTextPop();
            ShowLevelUpPopup(currentLevel);

            yield return null;
        }

        yield return AnimateSlider(0f, targetExp, targetExpNeeded);

        displayedLevel = targetLevel;
        displayedExp = targetExp;
        UpdateTexts(targetLevel, targetExp, targetExpNeeded);

        animateRoutine = null;
    }

    private IEnumerator AnimateSlider(float from, float to, int expNeeded)
    {
        if (expSlider == null)
            yield break;

        expSlider.minValue = 0f;
        expSlider.maxValue = expNeeded;

        float value = from;
        expSlider.value = value;

        while (Mathf.Abs(value - to) > 0.01f)
        {
            value = Mathf.MoveTowards(value, to, fillSpeed * DeltaTime());
            displayedExp = value;

            expSlider.value = value;
            UpdateTexts(displayedLevel, Mathf.RoundToInt(value), expNeeded);

            yield return null;
        }

        displayedExp = to;
        expSlider.value = to;
        UpdateTexts(displayedLevel, Mathf.RoundToInt(to), expNeeded);
    }

    private void PlayLevelTextPop()
    {
        if (levelText == null)
            return;

        if (levelPopRoutine != null)
            StopCoroutine(levelPopRoutine);

        levelPopRoutine = StartCoroutine(LevelTextPopRoutine());
    }

    private IEnumerator LevelTextPopRoutine()
    {
        RectTransform rt = levelText.rectTransform;

        Vector3 startScale = levelTextBaseScale;
        Vector3 targetScale = levelTextBaseScale * popScaleMultiplier;

        float halfDuration = popDuration * 0.5f;
        float t = 0f;

        while (t < halfDuration)
        {
            t += DeltaTime();
            float p = EaseOutBack(Mathf.Clamp01(t / halfDuration));
            rt.localScale = Vector3.LerpUnclamped(startScale, targetScale, p);
            yield return null;
        }

        t = 0f;

        while (t < halfDuration)
        {
            t += DeltaTime();
            float p = EaseInOut(Mathf.Clamp01(t / halfDuration));
            rt.localScale = Vector3.LerpUnclamped(targetScale, startScale, p);
            yield return null;
        }

        rt.localScale = levelTextBaseScale;
        levelPopRoutine = null;
    }

    private void ShowLevelUpPopup(int level)
    {
        if (levelUpPopup == null)
            return;

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(LevelUpPopupRoutine(level));
    }

    private IEnumerator LevelUpPopupRoutine(int level)
    {
        levelUpPopup.SetActive(true);

        if (levelUpPopupText != null)
            levelUpPopupText.text = $"Level Up! Level {level}";

        CanvasGroup group = levelUpPopup.GetComponent<CanvasGroup>();
        if (group == null)
            group = levelUpPopup.AddComponent<CanvasGroup>();

        RectTransform rt = levelUpPopup.GetComponent<RectTransform>();
        Vector3 baseScale = rt != null ? rt.localScale : Vector3.one;
        Vector3 startScale = baseScale * 1.08f;

        group.alpha = 1f;

        if (rt != null)
            rt.localScale = startScale;

        float t = 0f;
        while (t < popupDuration)
        {
            t += DeltaTime();
            float p = Mathf.Clamp01(t / popupDuration);

            if (rt != null)
                rt.localScale = Vector3.LerpUnclamped(startScale, baseScale, EaseOutBack(Mathf.Clamp01(p * 1.2f)));

            if (p > 0.45f)
                group.alpha = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.45f, 1f, p));

            yield return null;
        }

        group.alpha = 1f;

        if (rt != null)
            rt.localScale = baseScale;

        levelUpPopup.SetActive(false);
        popupRoutine = null;
    }

    private void ApplyImmediate(int level, int currentExp, int expNeeded)
    {
        SetSliderInstant(currentExp, expNeeded);
        UpdateTexts(level, currentExp, expNeeded);

        if (levelText != null)
            levelText.rectTransform.localScale = levelTextBaseScale;

        if (levelUpPopup != null)
            levelUpPopup.SetActive(false);
    }

    private void SetSliderInstant(float value, int expNeeded)
    {
        if (expSlider == null)
            return;

        expSlider.minValue = 0f;
        expSlider.maxValue = expNeeded;
        expSlider.value = value;
    }

    private void UpdateTexts(int level, int currentExp, int expNeeded)
    {
        if (levelText != null)
            levelText.text = $"{level}";

        if (expText != null)
            expText.text = $"{currentExp} / {expNeeded} XP";
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private object Wait(float seconds)
    {
        return useUnscaledTime
            ? new WaitForSecondsRealtime(seconds)
            : new WaitForSeconds(seconds);
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}