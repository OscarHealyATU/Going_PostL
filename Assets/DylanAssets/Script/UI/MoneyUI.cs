using System.Collections;
using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text valueText;

    [Header("Formatting")]
    [SerializeField] private string label = "Money:";
    [SerializeField] private string prefix = "€";

    [Header("Count Animation")]
    [SerializeField] private float countDuration = 0.4f;

    [Header("Flash")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color earnColor = Color.green;
    [SerializeField] private Color spendColor = Color.red;
    [SerializeField] private float flashDuration = 0.35f;

    private double lastAmount;
    private double displayedAmount;
    private Coroutine flashRoutine;
    private Coroutine countRoutine;

    private IEnumerator Start()
    {
        while (DbBoot.Instance == null)
            yield return null;

        while (labelText == null || valueText == null)
            yield return null;

        labelText.text = label;
        labelText.color = normalColor;

        lastAmount = PlayerService.Get().money;
        displayedAmount = lastAmount;
        UpdateMoney(displayedAmount, true);

        PlayerService.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDestroy()
    {
        PlayerService.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(double newAmount)
    {
        bool shouldFlash = true;
        Color targetFlashColor = normalColor;

        if (newAmount > lastAmount)
            targetFlashColor = earnColor;
        else if (newAmount < lastAmount)
            targetFlashColor = spendColor;
        else
            shouldFlash = false;

        if (countRoutine != null)
            StopCoroutine(countRoutine);

        countRoutine = StartCoroutine(AnimateMoney(displayedAmount, newAmount));

        lastAmount = newAmount;

        if (shouldFlash)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(FlashColor(targetFlashColor));
        }
    }

    private void UpdateMoney(double amount, bool setNormalColor)
    {
        if (valueText == null) return;

        valueText.text = $"{prefix}{amount:N0}";

        if (setNormalColor)
            valueText.color = normalColor;
    }

    private IEnumerator AnimateMoney(double fromAmount, double toAmount)
    {
        if (valueText == null)
            yield break;

        if (Mathf.Approximately(countDuration, 0f))
        {
            displayedAmount = toAmount;
            UpdateMoney(displayedAmount, false);
            countRoutine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < countDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / countDuration);

            displayedAmount = Mathf.Lerp((float)fromAmount, (float)toAmount, t);
            UpdateMoney(displayedAmount, false);

            yield return null;
        }

        displayedAmount = toAmount;
        UpdateMoney(displayedAmount, false);
        countRoutine = null;
    }

    private IEnumerator FlashColor(Color flashColor)
    {
        if (valueText == null)
            yield break;

        valueText.color = flashColor;

        float timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            valueText.color = Color.Lerp(flashColor, normalColor, timer / flashDuration);
            yield return null;
        }

        valueText.color = normalColor;
        flashRoutine = null;
    }
}