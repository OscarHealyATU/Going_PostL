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

    [Header("Level Text Pop")]
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private float popDuration = 0.18f;

    private Coroutine animateRoutine;
    private Coroutine levelPopRoutine;

    private int displayedLevel = 1;
    private float displayedExp = 0f;

    private Vector3 levelTextBaseScale = Vector3.one;

    private void Start()
    {
        if (levelText != null)
            levelTextBaseScale = levelText.rectTransform.localScale;

        int level = PlayerService.GetLevel();
        int currentExp = PlayerService.GetExperienceIntoCurrentLevel();
        int expNeeded = PlayerService.GetExperienceNeededForNextLevel();

        displayedLevel = level;
        displayedExp = currentExp;

        ApplyImmediate(level, currentExp, expNeeded);

        PlayerService.OnExperienceChanged += HandleExperienceChanged;
    }

    private void OnDestroy()
    {
        PlayerService.OnExperienceChanged -= HandleExperienceChanged;
    }

    private void HandleExperienceChanged(int newLevel, int newCurrentExp, int expNeeded)
    {
        if (animateRoutine != null)
            StopCoroutine(animateRoutine);

        animateRoutine = StartCoroutine(AnimateExperienceChange(newLevel, newCurrentExp, expNeeded));
    }

    private IEnumerator AnimateExperienceChange(int targetLevel, int targetExp, int expNeeded)
    {
        if (targetLevel == displayedLevel)
        {
            yield return AnimateSlider(displayedExp, targetExp, expNeeded);
            displayedExp = targetExp;
            UpdateTexts(displayedLevel, Mathf.RoundToInt(displayedExp), expNeeded);
            animateRoutine = null;
            yield break;
        }

        while (displayedLevel < targetLevel)
        {
            yield return AnimateSlider(displayedExp, expNeeded, expNeeded);

            displayedExp = expNeeded;
            UpdateTexts(displayedLevel, expNeeded, expNeeded);

            yield return new WaitForSeconds(levelUpPause);

            displayedLevel++;
            displayedExp = 0f;

            if (expSlider != null)
                expSlider.value = 0f;

            UpdateTexts(displayedLevel, 0, expNeeded);
            PlayLevelTextPop();
        }

        yield return AnimateSlider(displayedExp, targetExp, expNeeded);

        displayedExp = targetExp;
        UpdateTexts(displayedLevel, Mathf.RoundToInt(displayedExp), expNeeded);

        animateRoutine = null;
    }

    private IEnumerator AnimateSlider(float from, float to, int expNeeded)
    {
        if (expSlider == null)
            yield break;

        expSlider.minValue = 0f;
        expSlider.maxValue = expNeeded;

        float value = from;

        while (Mathf.Abs(value - to) > 0.01f)
        {
            value = Mathf.MoveTowards(value, to, fillSpeed * Time.deltaTime);
            displayedExp = value;

            expSlider.value = value;
            UpdateTexts(displayedLevel, Mathf.RoundToInt(value), expNeeded);

            yield return null;
        }

        displayedExp = to;
        expSlider.value = to;
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
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / halfDuration);
            rt.localScale = Vector3.Lerp(startScale, targetScale, p);
            yield return null;
        }

        t = 0f;

        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / halfDuration);
            rt.localScale = Vector3.Lerp(targetScale, startScale, p);
            yield return null;
        }

        rt.localScale = levelTextBaseScale;
        levelPopRoutine = null;
    }

    private void ApplyImmediate(int level, int currentExp, int expNeeded)
    {
        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = expNeeded;
            expSlider.value = currentExp;
        }

        UpdateTexts(level, currentExp, expNeeded);

        if (levelText != null)
            levelText.rectTransform.localScale = levelTextBaseScale;
    }

    private void UpdateTexts(int level, int currentExp, int expNeeded)
    {
        if (levelText != null)
            levelText.text = $"{level}";

        if (expText != null)
            expText.text = $"{currentExp} / {expNeeded} XP";
    }
}